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