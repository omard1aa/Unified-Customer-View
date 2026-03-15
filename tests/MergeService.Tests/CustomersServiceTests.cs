using MergeService.Models;
using MergeService.Services;

namespace MergeService.Tests
{
    public class CustomersServiceTests
    {
        private readonly CustomersService _service;

        public CustomersServiceTests()
        {
            // Pass nulls for HTTP clients and logger — we're testing pure merge logic only
            _service = new CustomersService(null!, null!, null!);
        }

        // ─── Helpers ────────────────────────────────────────────────────────────────

        private static SystemACustomer MakeSystemA(
            string id = "legacy_001",
            string email = "test@example.de",
            string name = "Test User",
            string address = "Street 1, 10115 Berlin",
            string contractType = "RENTAL",
            string contractStart = "2021-03-15",
            string lastUpdated = "2024-11-01T10:00:00Z") => new()
        {
            Id = id,
            email = email,
            name = name,
            address = address,
            ContractType = contractType,
            ContractStartDate = DateTime.Parse(contractStart),
            last_updated = DateTime.Parse(lastUpdated)
        };

        private static SystemBCustomer MakeSystemB(
            string uuid = "modern_101",
            string email = "test@example.de",
            string name = "Test User",
            string address = "Street 1, 10115 Berlin",
            string phone = "+49 170 000 0000",
            string lastUpdated = "2025-01-10T08:00:00Z") => new()
        {
            uuid = uuid,
            email = email,
            name = name,
            address = address,
            phone = phone,
            last_updated = DateTime.Parse(lastUpdated)
        };

        // ─── MergeFromBoth ───────────────────────────────────────────────────────────

        [Fact]
        public void MergeFromBoth_NoConflicts_ReturnsCorrectSources()
        {
            var systemA = MakeSystemA(
                id: "legacy_002",
                email: "erika.muster@example.de",
                name: "Erika Musterfrau",
                address: "Hauptstr. 42, 10115 Berlin",
                contractType: "PURCHASE",
                contractStart: "2022-07-01",
                lastUpdated: "2024-08-15T14:30:00Z");

            var systemB = MakeSystemB(
                uuid: "modern_102",
                email: "erika.muster@example.de",
                name: "Erika Musterfrau",
                address: "Hauptstr. 42, 10115 Berlin",
                phone: "+49 171 987 6543",
                lastUpdated: "2024-09-01T11:00:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            Assert.Equal("erika.muster@example.de", result.email);
            Assert.Equal("Erika Musterfrau", result.name);
            Assert.Equal("+49 171 987 6543", result.phone);
            Assert.Equal("Hauptstr. 42, 10115 Berlin", result.address);
            Assert.Equal("PURCHASE", result.ContractType);
            Assert.Equal("SystemB", result.Metadata!.sources!["address"]);
            Assert.Equal("SystemA", result.Metadata!.sources!["contractType"]);
            Assert.Empty(result.Metadata!.conflicts!);
            Assert.False(result.Metadata!.isPartial);
        }

        [Fact]
        public void MergeFromBoth_AddressConflict_SystemBAddressUsed()
        {
            // max.mustermann: both systems, different address, SystemB is newer
            var systemA = MakeSystemA(
                id: "legacy_001",
                email: "max.mustermann@example.de",
                address: "Sonnenallee 1, 12345 Berlin",
                lastUpdated: "2024-11-01T10:00:00Z");

            var systemB = MakeSystemB(
                uuid: "modern_101",
                email: "max.mustermann@example.de",
                address: "Sonnenallee 1a, 12345 Berlin",
                lastUpdated: "2025-01-10T08:00:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            // SystemB wins for address per priority rules
            Assert.Equal("Sonnenallee 1a, 12345 Berlin", result.address);
            Assert.Equal("SystemB", result.Metadata!.sources!["address"]);

            var conflict = result.Metadata!.conflicts!.Single(c => c["field"] == "address");
            Assert.Equal("Sonnenallee 1, 12345 Berlin", conflict["systemAValue"]);
            Assert.Equal("Sonnenallee 1a, 12345 Berlin", conflict["systemBValue"]);
            Assert.Equal("SystemB", conflict["resolvedFrom"]);
        }

        [Fact]
        public void MergeFromBoth_NameConflict_NewerSystemBWins()
        {
            // sophie.mueller: name variation, SystemB is newer
            var systemA = MakeSystemA(
                name: "Sophie Muller",
                lastUpdated: "2024-10-05T16:00:00Z");

            var systemB = MakeSystemB(
                name: "Sophie Mueller",
                lastUpdated: "2025-02-01T09:30:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            // SystemB is newer → its name should be used
            Assert.Equal("Sophie Mueller", result.name);
            Assert.Equal("SystemB", result.Metadata!.sources!["name"]);

            var conflict = result.Metadata!.conflicts!.Single(c => c["field"] == "name");
            Assert.Equal("Sophie Muller", conflict["systemAValue"]);
            Assert.Equal("Sophie Mueller", conflict["systemBValue"]);
            Assert.Equal("SystemB", conflict["resolvedFrom"]);
        }

        [Fact]
        public void MergeFromBoth_NameConflict_NewerSystemAWins()
        {
            var systemA = MakeSystemA(
                name: "Jan Schmidt",
                lastUpdated: "2025-05-01T10:00:00Z");  // A is newer

            var systemB = MakeSystemB(
                name: "J. Schmidt",
                lastUpdated: "2024-01-01T08:00:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            Assert.Equal("Jan Schmidt", result.name);
            Assert.Equal("SystemA", result.Metadata!.sources!["name"]);

            var conflict = result.Metadata!.conflicts!.Single(c => c["field"] == "name");
            Assert.Equal("SystemA", conflict["resolvedFrom"]);
        }

        [Fact]
        public void MergeFromBoth_BothNameAndAddressConflict_TwoConflictsReported()
        {
            var systemA = MakeSystemA(
                name: "Sophie Muller",
                address: "Kastanienallee 7, 10435 Berlin",
                lastUpdated: "2024-10-05T16:00:00Z");

            var systemB = MakeSystemB(
                name: "Sophie Mueller",
                address: "Kastanienallee 7a, 10435 Berlin",
                lastUpdated: "2025-02-01T09:30:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            Assert.Equal(2, result.Metadata!.conflicts!.Count);
            Assert.Contains(result.Metadata.conflicts, c => c["field"] == "name");
            Assert.Contains(result.Metadata.conflicts, c => c["field"] == "address");
            Assert.False(result.Metadata.isPartial);
        }

        [Fact]
        public void MergeFromBoth_PhoneAlwaysFromSystemB_ContractAlwaysFromSystemA()
        {
            var systemA = MakeSystemA(contractType: "PURCHASE", contractStart: "2022-01-01");
            var systemB = MakeSystemB(phone: "+49 172 555 0000");

            var result = _service.MergeFromBoth(systemA, systemB);

            Assert.Equal("+49 172 555 0000", result.phone);
            Assert.Equal("PURCHASE", result.ContractType);
            Assert.Equal(DateTime.Parse("2022-01-01"), result.ContractStartDate);
            Assert.Equal("SystemB", result.Metadata!.sources!["phone"]);
            Assert.Equal("SystemA", result.Metadata!.sources!["contractStartDate"]);
            Assert.Equal("SystemA", result.Metadata!.sources!["contractType"]);
        }

        [Fact]
        public void MergeFromBoth_LastUpdated_TakesNewerTimestamp()
        {
            var systemA = MakeSystemA(lastUpdated: "2024-01-01T00:00:00Z");
            var systemB = MakeSystemB(lastUpdated: "2025-06-01T00:00:00Z");

            var result = _service.MergeFromBoth(systemA, systemB);

            // last_updated should reflect the newer of the two
            Assert.NotNull(result.last_updated);
            Assert.Contains("2025", result.last_updated);
        }

        [Fact]
        public void MergeFromBoth_BothIdsPreserved()
        {
            var systemA = MakeSystemA(id: "legacy_004");
            var systemB = MakeSystemB(uuid: "modern_104");

            var result = _service.MergeFromBoth(systemA, systemB);

            Assert.Equal("legacy_004", result.SystemAId);
            Assert.Equal("modern_104", result.SystemBuuid);
        }

        // ─── MergeFromSystemA ────────────────────────────────────────────────────────

        [Fact]
        public void MergeFromSystemA_NotPartial_ReturnsSystemAData()
        {
            // jan.schmidt: only in System A (System B simply has no record)
            var systemA = MakeSystemA(
                id: "legacy_003",
                email: "jan.schmidt@example.de",
                name: "Jan Schmidt",
                address: "Berliner Str. 10, 80331 Munich",
                contractType: "RENTAL",
                contractStart: "2023-01-10");

            var result = _service.MergeFromSystemA(systemA, isPartial: false);

            Assert.Equal("jan.schmidt@example.de", result.email);
            Assert.Equal("Jan Schmidt", result.name);
            Assert.Equal("RENTAL", result.ContractType);
            Assert.Equal("legacy_003", result.SystemAId);
            Assert.Null(result.phone);
            Assert.Null(result.SystemBuuid);
            Assert.Equal("System A", result.Metadata!.sources!["all"]);
            Assert.False(result.Metadata!.isPartial);
        }

        [Fact]
        public void MergeFromSystemA_WhenSystemBUnavailable_IsPartialTrue()
        {
            var systemA = MakeSystemA();

            var result = _service.MergeFromSystemA(systemA, isPartial: true);

            Assert.True(result.Metadata!.isPartial);
            Assert.Equal("System A", result.Metadata.sources!["all"]);
        }

        // ─── MergeFromSystemB ────────────────────────────────────────────────────────

        [Fact]
        public void MergeFromSystemB_OnlyInB_ReturnsSystemBData()
        {
            // lisa.neu: only in System B
            var systemB = MakeSystemB(
                uuid: "modern_103",
                email: "lisa.neu@example.de",
                name: "Lisa Neumann",
                address: "Friedrichstr. 99, 10117 Berlin",
                phone: "+49 172 555 0000");

            var result = _service.MergeFromSystemB(systemB, isPartial: false);

            Assert.Equal("lisa.neu@example.de", result.email);
            Assert.Equal("Lisa Neumann", result.name);
            Assert.Equal("+49 172 555 0000", result.phone);
            Assert.Equal("modern_103", result.SystemBuuid);
            Assert.Null(result.ContractType);
            Assert.Null(result.SystemAId);
            Assert.Equal("System B", result.Metadata!.sources!["all"]);
            Assert.False(result.Metadata.isPartial);
        }

        // ─── SyncBothSystems ─────────────────────────────────────────────────────────

        [Fact]
        public void SyncBothSystems_NoConflicts_StatusNotConflicted()
        {
            var systemA = MakeSystemA(
                email: "erika.muster@example.de",
                name: "Erika Musterfrau",
                address: "Hauptstr. 42, 10115 Berlin",
                lastUpdated: "2024-08-15T14:30:00Z");

            var systemB = MakeSystemB(
                email: "erika.muster@example.de",
                name: "Erika Musterfrau",
                address: "Hauptstr. 42, 10115 Berlin",
                lastUpdated: "2024-09-01T11:00:00Z");

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("Not_Conflicted", result.Status);
            Assert.Equal("erika.muster@example.de", result.Email);
            Assert.Equal("match", result.Fields!["name"].Status);
            Assert.Equal("match", result.Fields!["address"].Status);
        }

        [Fact]
        public void SyncBothSystems_WithAddressConflict_StatusConflicted()
        {
            var systemA = MakeSystemA(
                email: "max.mustermann@example.de",
                address: "Sonnenallee 1, 12345 Berlin",
                lastUpdated: "2024-11-01T10:00:00Z");

            var systemB = MakeSystemB(
                email: "max.mustermann@example.de",
                address: "Sonnenallee 1a, 12345 Berlin",
                lastUpdated: "2025-01-10T08:00:00Z");

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("Conflicted", result.Status);
            Assert.True(result.Fields!.ContainsKey("address"));
            Assert.Equal("Sonnenallee 1, 12345 Berlin", result.Fields["address"].SystemAValue);
            Assert.Equal("Sonnenallee 1a, 12345 Berlin", result.Fields["address"].SystemBValue);
        }

        [Fact]
        public void SyncBothSystems_NewerSource_SystemBWhenBIsNewer()
        {
            var systemA = MakeSystemA(lastUpdated: "2024-01-01T00:00:00Z");
            var systemB = MakeSystemB(lastUpdated: "2025-01-01T00:00:00Z");

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("SystemB", result.NewerSource);
        }

        [Fact]
        public void SyncBothSystems_NewerSource_SystemAWhenAIsNewer()
        {
            var systemA = MakeSystemA(lastUpdated: "2025-06-01T00:00:00Z");
            var systemB = MakeSystemB(lastUpdated: "2024-01-01T00:00:00Z");

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("SystemA", result.NewerSource);
        }

        [Fact]
        public void SyncBothSystems_PhoneOnlyInB_ReportedAsOnlyInB()
        {
            var systemA = MakeSystemA();
            var systemB = MakeSystemB(phone: "+49 170 123 4567");

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("only_in_b", result.Fields!["phone"].Status);
            Assert.Null(result.Fields["phone"].SystemAValue);
            Assert.Equal("+49 170 123 4567", result.Fields["phone"].SystemBValue);
        }

        [Fact]
        public void SyncBothSystems_ContractFieldsOnlyInA_ReportedAsOnlyInA()
        {
            var systemA = MakeSystemA(contractType: "RENTAL", contractStart: "2021-03-15");
            var systemB = MakeSystemB();

            var result = _service.SyncBothSystems(systemA, systemB);

            Assert.Equal("only_in_a", result.Fields!["contractType"].Status);
            Assert.Equal("only_in_a", result.Fields!["contractStartDate"].Status);
            Assert.Null(result.Fields["contractType"].SystemBValue);
        }

        // ─── CreateConflictForName ───────────────────────────────────────────────────

        [Fact]
        public void CreateConflictForName_AddsConflictEntry()
        {
            var systemA = MakeSystemA(name: "Sophie Muller", lastUpdated: "2024-10-05T16:00:00Z");
            var systemB = MakeSystemB(name: "Sophie Mueller", lastUpdated: "2025-02-01T09:30:00Z");

            var merged = new UnifiedCustomerRecord
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = new MetaData
                {
                    sources = [],
                    conflicts = []
                }
            };

            var result = _service.CreateConflictForName(merged, systemA, systemB);

            Assert.Single(result.Metadata!.conflicts!);
            var conflict = result.Metadata.conflicts![0];
            Assert.Equal("name", conflict["field"]);
            Assert.Equal("Sophie Muller", conflict["systemAValue"]);
            Assert.Equal("Sophie Mueller", conflict["systemBValue"]);
        }

        [Fact]
        public void CreateConflictForName_SystemBNewer_UsesBName()
        {
            var systemA = MakeSystemA(name: "Sophie Muller", lastUpdated: "2024-10-05T16:00:00Z");
            var systemB = MakeSystemB(name: "Sophie Mueller", lastUpdated: "2025-02-01T09:30:00Z");

            var merged = new UnifiedCustomerRecord
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = new MetaData { sources = [], conflicts = [] }
            };

            var result = _service.CreateConflictForName(merged, systemA, systemB);

            Assert.Equal("Sophie Mueller", result.name);
            Assert.Equal("SystemB", result.Metadata!.sources!["name"]);
            Assert.Equal("SystemB", result.Metadata.conflicts![0]["resolvedFrom"]);
        }

        [Fact]
        public void CreateConflictForName_SystemANewer_UsesAName()
        {
            var systemA = MakeSystemA(name: "Jan Schmidt", lastUpdated: "2025-05-01T10:00:00Z");
            var systemB = MakeSystemB(name: "J. Schmidt", lastUpdated: "2024-01-01T08:00:00Z");

            var merged = new UnifiedCustomerRecord
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = new MetaData { sources = [], conflicts = [] }
            };

            var result = _service.CreateConflictForName(merged, systemA, systemB);

            Assert.Equal("Jan Schmidt", result.name);
            Assert.Equal("SystemA", result.Metadata!.sources!["name"]);
            Assert.Equal("SystemA", result.Metadata.conflicts![0]["resolvedFrom"]);
        }

        // ─── CreateConflictForAddress ────────────────────────────────────────────────

        [Fact]
        public void CreateConflictForAddress_AlwaysUsesSystemBAddress()
        {
            var systemA = MakeSystemA(address: "Sonnenallee 1, 12345 Berlin");
            var systemB = MakeSystemB(address: "Sonnenallee 1a, 12345 Berlin");

            var merged = new UnifiedCustomerRecord
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = new MetaData { sources = [], conflicts = [] }
            };

            var result = _service.CreateConflictForAddress(merged, systemA, systemB);

            Assert.Single(result.Metadata!.conflicts!);
            var conflict = result.Metadata.conflicts![0];
            Assert.Equal("address", conflict["field"]);
            Assert.Equal("Sonnenallee 1, 12345 Berlin", conflict["systemAValue"]);
            Assert.Equal("Sonnenallee 1a, 12345 Berlin", conflict["systemBValue"]);
            Assert.Equal("SystemB", conflict["resolvedFrom"]);
            Assert.Equal("SystemB", result.Metadata.sources!["address"]);
        }

        [Fact]
        public void CreateConflictForAddress_NullAddress_HandledGracefully()
        {
            var systemA = MakeSystemA(address: "Some Street");
            var systemB = MakeSystemB(address: "Other Street");
            systemA.address = null;

            var merged = new UnifiedCustomerRecord
            {
                Id = Guid.NewGuid().ToString(),
                Metadata = new MetaData { sources = [], conflicts = [] }
            };

            var result = _service.CreateConflictForAddress(merged, systemA, systemB);

            Assert.Equal("null", result.Metadata!.conflicts![0]["systemAValue"]);
        }
    }
}
