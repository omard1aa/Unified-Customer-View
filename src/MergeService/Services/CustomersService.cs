using MergeService.Clients;
using MergeService.Models;
using MergeService.Interfaces;
using System.Text.Json;

namespace MergeService.Services
{
    public class CustomersService : ICustomerService
    {
        SystemAClient _systemAClient;
        SystemBClient _systemBClient;
        ILogger<CustomersService> _logger;
        public CustomersService(SystemAClient systemAClient, SystemBClient systemBClient, ILogger<CustomersService> logger)
        {
            _systemAClient = systemAClient;
            _systemBClient = systemBClient;
            _logger = logger;
        }

        public async Task<UnifiedCustomerRecord?> GetUnifiedCustomerByEmailAsync(string email)
        {
            _logger.LogInformation("[MergeService] Getting customer by email: {Email}", email);
            
            SystemACustomer? systemACustomer = await _systemAClient.GetByEmailAsync<SystemACustomer>(email);
            SystemBCustomer? systemBCustomer = null;
            bool systemBUnavailable = false;
            try
            {
                systemBCustomer = await _systemBClient.GetByEmailAsync<SystemBCustomer>(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemB unavailable for {Email}", email);
                systemBUnavailable = true;
            }

            // Neither system has this customer
            if (systemACustomer == null && systemBCustomer == null && !systemBUnavailable)
                return null;

            // Both systems have data
            if (systemACustomer != null && systemBCustomer != null)
                return MergeFromBoth(systemACustomer, systemBCustomer);

            // Only System A
            if (systemACustomer != null)
                return MergeFromSystemA(systemACustomer, isPartial: systemBUnavailable);

            // Only System B
            if (systemBCustomer != null)
                return MergeFromSystemB(systemBCustomer, false);

            return null;
        }

        public async Task<List<UnifiedCustomerRecord>> SearchAsync(string query)
        {
            // For simplicity, we will search in both systems and return the first match found, in real scenario we might want to return multiple results with relevance score
            List<SystemACustomer>? systemACustomers = await _systemAClient.SearchAsync<SystemACustomer>(query);
            List<SystemBCustomer>? systemBCustomers = null;
            List<UnifiedCustomerRecord> mergedCustomers = new List<UnifiedCustomerRecord>();
            bool systemBUnavailable = false;
            try
            {
                systemBCustomers = await _systemBClient.SearchAsync<SystemBCustomer>(query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemB unavailable for {query}", query);
                systemBUnavailable = true;
            }

            // Neither system has this customer
            if (systemACustomers.Count == 0  && systemBCustomers?.Count == 0 && !systemBUnavailable)
                return new List<UnifiedCustomerRecord>();

            // convert the lists to dictionary (email, customerRecord)
            var systemARecordsDict = (systemACustomers ?? new List<SystemACustomer>()).Where(c => c.email != null).ToDictionary(c => c.email!, c => c);
            var systemBRecordsDict = (systemBCustomers ?? new List<SystemBCustomer>()).Where(c => c.email != null).ToDictionary(c => c.email!, c => c);
            
            var distinctEmails = new HashSet<string>(systemARecordsDict.Keys);
            distinctEmails.UnionWith(systemBRecordsDict.Keys);

            foreach(var email in distinctEmails)
            {
                systemARecordsDict.TryGetValue(email, out var systemARecord);
                systemBRecordsDict.TryGetValue(email, out var systemBRecord);

                if(systemARecord != null && systemBRecord != null)
                {
                    mergedCustomers.Add(MergeFromBoth(systemARecord, systemBRecord));
                }
                else if(systemARecord != null)
                {
                    mergedCustomers.Add(MergeFromSystemA(systemARecord, isPartial: systemBUnavailable));
                }
                else if(systemBRecord != null)
                {
                    mergedCustomers.Add(MergeFromSystemB(systemBRecord, isPartial: false));
                }
            }
            return mergedCustomers;
        }

        public async Task<SyncRecord> Sync(string email)
        {
            SystemACustomer? systemACustomer = await _systemAClient.GetByEmailAsync<SystemACustomer>(email);
            SystemBCustomer? systemBCustomer = null;
            var syncedRecord = new SyncRecord();
            try
            {
                systemBCustomer = await _systemBClient.GetByEmailAsync<SystemBCustomer>(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemB unavailable for {Email}", email);
                return new SyncRecord { Email = email, Status = "system_b_unavailable" };
            }
            if (systemACustomer == null && systemBCustomer == null)
                return new SyncRecord { Email = email, Status = "not_found" };
            if (systemACustomer != null && systemBCustomer == null)
                return new SyncRecord { Email = email, Status = "only_in_a" };
            if (systemACustomer == null && systemBCustomer != null)
                return new SyncRecord { Email = email, Status = "only_in_b" };

            return SyncBothSystems(systemACustomer!, systemBCustomer!);
        }

        public async Task<(bool SystemA, bool SystemB)> IsHealthyAsync()
        {
            bool systemAHealthy;
            bool systemBHealthy;
            try
            {
                systemAHealthy = await _systemAClient.IsHealthyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemA health check failed.");
                systemAHealthy = false;
            }
            try
            {
                systemBHealthy = await _systemBClient.IsHealthyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemB health check failed.");
                systemBHealthy = false;
            }
            return (systemAHealthy, systemBHealthy);
        }

        // Helper functions start from here
        public SyncRecord SyncBothSystems(SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {            
            var mergedRecord = MergeFromBoth(systemACustomer, systemBCustomer);
            var syncedRecord = new SyncRecord();
            syncedRecord.Fields = new Dictionary<string, Report>();
            syncedRecord.Email = mergedRecord.email;
            syncedRecord.Status = "Not_Conflicted";
            syncedRecord.NewerSource = systemACustomer.last_updated > systemBCustomer.last_updated ? "SystemA" : "SystemB";
            syncedRecord.Fields["name"] = new Report { SystemAValue = systemACustomer.name, SystemBValue = systemBCustomer.name, Status = "match" };
            syncedRecord.Fields["phone"] = new Report { SystemAValue = null, SystemBValue = systemBCustomer.phone, Status = "only_in_b" };
            syncedRecord.Fields["contractStartDate"] = new Report { SystemAValue = systemACustomer.ContractStartDate?.ToString(), SystemBValue = null, Status = "only_in_a" };
            syncedRecord.Fields["contractType"] = new Report { SystemAValue = systemACustomer.ContractType, SystemBValue = null, Status = "only_in_a" };
            syncedRecord.Fields["address"] = new Report { SystemAValue = systemACustomer.address, SystemBValue = systemBCustomer.address, Status = "match" };
            if (mergedRecord != null && mergedRecord.Metadata != null && mergedRecord.Metadata.conflicts != null)
            {
                foreach(var conflict in mergedRecord.Metadata.conflicts)
                {
                    var fieldName = conflict["field"];
                    syncedRecord.Fields[fieldName] = new Report
                    {
                        SystemAValue = conflict["systemAValue"],
                        SystemBValue = conflict["systemBValue"],
                        Status = conflict["resolvedFrom"],
                    };
                    syncedRecord.Status = "Conflicted";
                }
            }
            return syncedRecord;
        }
        
        public UnifiedCustomerRecord MergeFromSystemA(SystemACustomer systemACustomer, bool isPartial)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = Guid.NewGuid().ToString(),
                SystemAId = systemACustomer.Id,
                email = systemACustomer.email,
                name = systemACustomer.name,
                address = systemACustomer.address,
                ContractStartDate = systemACustomer.ContractStartDate,
                ContractType = systemACustomer.ContractType,
                last_updated = systemACustomer.last_updated?.ToString("o"),
                Metadata = new MetaData
                { 
                    sources = new Dictionary<string, string> { { "all", "System A" } },
                    isPartial = isPartial
                }            
            };

            return mergedCustomerRecord;
        }

        public UnifiedCustomerRecord MergeFromSystemB(SystemBCustomer systemBCustomer, bool isPartial)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = Guid.NewGuid().ToString(),
                SystemBuuid = systemBCustomer.uuid,
                email = systemBCustomer.email,
                name = systemBCustomer.name,
                address = systemBCustomer.address,
                phone = systemBCustomer.phone,
                last_updated = systemBCustomer.last_updated.ToString("o"),
                Metadata = new MetaData
                { 
                    sources = new Dictionary<string, string> { { "all", "System B" } },
                    isPartial = isPartial
                }
            };
            return mergedCustomerRecord;
        }
    
        public UnifiedCustomerRecord MergeFromBoth(SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = Guid.NewGuid().ToString(),
                SystemAId = systemACustomer.Id,
                SystemBuuid = systemBCustomer.uuid,
                email = systemACustomer.email,
                name = systemACustomer.name,           // default to A
                phone = systemBCustomer.phone,          // always from B
                address = systemBCustomer.address,      // from B (higher priority)
                ContractStartDate = systemACustomer.ContractStartDate,  // always from A
                ContractType = systemACustomer.ContractType,  // always from A
                Metadata = new MetaData
                {
                    sources = new Dictionary<string, string>
                    {
                        { "name", "SystemA" },
                        { "phone", "SystemB" },
                        { "address", "SystemB" },
                        { "contractStartDate", "SystemA" },
                        { "contractType", "SystemA" }
                    },                    
                    conflicts = new List<Dictionary<string, string>>(),
                    isPartial = false
                },
                last_updated = systemBCustomer.last_updated > systemACustomer.last_updated 
                                ? systemBCustomer.last_updated.ToString("o") 
                                : systemACustomer.last_updated?.ToString("o"),
            };
            if(systemACustomer.name != systemBCustomer.name)
            {
                mergedCustomerRecord = CreateConflictForName(mergedCustomerRecord, systemACustomer, systemBCustomer);
            }
            if(systemACustomer.address != systemBCustomer.address)
            {
                mergedCustomerRecord = CreateConflictForAddress(mergedCustomerRecord, systemACustomer, systemBCustomer);
            }

            return mergedCustomerRecord;
        }
    
        public UnifiedCustomerRecord CreateConflictForAddress(UnifiedCustomerRecord mergedCustomerRecord, SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {
            mergedCustomerRecord.Metadata = mergedCustomerRecord.Metadata ?? new MetaData();
            mergedCustomerRecord.Metadata.conflicts = mergedCustomerRecord.Metadata.conflicts ?? new List<Dictionary<string, string>>();
            mergedCustomerRecord.Metadata.conflicts.Add(new Dictionary<string, string> 
            {
                {"field", "address"},
                {"systemAValue", systemACustomer.address ?? "null" },
                {"systemBValue", systemBCustomer.address ?? "null"},
                {"resolvedFrom", "SystemB" }
            });
            mergedCustomerRecord.Metadata.sources = mergedCustomerRecord.Metadata.sources ?? new Dictionary<string, string>();
            mergedCustomerRecord.Metadata.sources["address"] = "SystemB";
            return mergedCustomerRecord;
        }
    
        public UnifiedCustomerRecord CreateConflictForName(UnifiedCustomerRecord mergedCustomerRecord, SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {
            string newerSource = systemACustomer.last_updated > systemBCustomer.last_updated ? "SystemA" : "SystemB";
            mergedCustomerRecord.name = newerSource == "SystemA" ? systemACustomer.name : systemBCustomer.name;
            mergedCustomerRecord.Metadata = mergedCustomerRecord.Metadata ?? new MetaData();
            mergedCustomerRecord.Metadata.conflicts = mergedCustomerRecord.Metadata.conflicts ?? new List<Dictionary<string, string>>();
            mergedCustomerRecord.Metadata.conflicts.Add(new Dictionary<string, string> 
            {
                {"field", "name"},
                {"systemAValue", systemACustomer.name ?? "null" },
                {"systemBValue", systemBCustomer.name ?? "null"},
                {"resolvedFrom", newerSource }
            });
            mergedCustomerRecord.Metadata.sources = mergedCustomerRecord.Metadata.sources ?? new Dictionary<string, string>();
            mergedCustomerRecord.Metadata.sources["name"] = newerSource;
            mergedCustomerRecord.Metadata.isPartial = false;
            return mergedCustomerRecord;
        }
        
        /*
         For the Conflict Creation methods, we can have enhancement here, make it generic: CreateConflict()
         and we pass the conflicted field name, at this moment we would need priority rules engine to determine the resolved value, 
         and we can enhance it to be more dynamic and support more complex rules in the future, but for now we can keep it simple as per the requirements.
        */    
    }
}