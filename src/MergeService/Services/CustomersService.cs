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
            this._systemAClient = systemAClient;
            this._systemBClient = systemBClient;
            this._logger = logger;
        }
        public async Task<UnifiedCustomerRecord?> GetUnifiedCustomerByEmailAsync(string email)
        {
            var unifiedCustomerRecord = new UnifiedCustomerRecord { Id = Guid.NewGuid().ToString() };
            _logger.LogInformation("[MergeService] Getting customer by email: {Email}", email);
            try
            {
                var systemACustomer = await _systemAClient.GetByEmailAsync<SystemACustomer>(email);
                var systemBCustomer = await _systemBClient.GetByEmailAsync<SystemBCustomer>(email);
                if (systemACustomer == null && systemBCustomer == null)
                {
                    _logger.LogWarning($"[MergeService] No customer found with email: {email}");
                    return null;
                }

                // If SystemB is unavailable, we want to migrate from SystemA to unified view
                if(systemACustomer != null && systemBCustomer == null)
                {
                    _logger.LogInformation($"[MergeService] Customer found only in systemA: {JsonSerializer.Serialize(systemACustomer)} for email: {email}");
                    
                    // Migrate only from systemA to unified view, as SystemA is unavailable
                    unifiedCustomerRecord = MergeFromSystemA(systemACustomer);
                    
                    return unifiedCustomerRecord;
                }

                // If SystemA is unavailable, we want to migrate from SystemB to unified view
                if(systemACustomer == null && systemBCustomer != null)
                {
                    _logger.LogInformation($"[MergeService] Customer found only in systemB: {JsonSerializer.Serialize(systemBCustomer)} for email: {email}");
                    
                    // Migrate only from systemB to unified view, as System B is unavailable
                    unifiedCustomerRecord = MergeFromSystemB(systemBCustomer);
                    
                    return unifiedCustomerRecord;
                }

                // if both systems are available we want to merge from both, and resolve conflicts if needed
                if (systemACustomer != null && systemBCustomer != null)
                {
                    unifiedCustomerRecord = MergeFromBoth(systemACustomer!, systemBCustomer!);
                }

                _logger.LogInformation("[MergeService] Successfully merged customer record for email: {Email} (partial: {IsPartial})", email, unifiedCustomerRecord.Metadata.isPartial);
                return unifiedCustomerRecord;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MergeService] SystemB unavailable, proceeding with SystemA only.");
                throw ex;
            }
        }

        public UnifiedCustomerRecord ResolveConflict(SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {
            var resolvedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = new Guid().ToString(), 
                SystemAId = systemACustomer.Id, 
                SystemBuuid = systemBCustomer.uuid
            };


            return resolvedCustomerRecord;
        }

        public UnifiedCustomerRecord MergeFromSystemA(SystemACustomer systemACustomer)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = new Guid().ToString(),
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
                    isPartial = false
                }            };

            return mergedCustomerRecord;
        }

        public UnifiedCustomerRecord MergeFromSystemB(SystemBCustomer systemBCustomer)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = new Guid().ToString(),
                SystemBuuid = systemBCustomer.uuid,
                email = systemBCustomer.email,
                name = systemBCustomer.name,
                address = systemBCustomer.address,
                phone = systemBCustomer.phone,
                last_updated = systemBCustomer.last_updated.ToString("o"),
                Metadata = new MetaData
                { 
                    sources = new Dictionary<string, string> { { "all", "System B" } },
                    isPartial = false
                }
            };

            return mergedCustomerRecord;
        }
    
        public UnifiedCustomerRecord MergeFromBoth(SystemACustomer systemACustomer, SystemBCustomer systemBCustomer)
        {
            var mergedCustomerRecord = new UnifiedCustomerRecord()
            {
                Id = new Guid().ToString(),
                phone = systemBCustomer.phone,
                ContractStartDate = systemACustomer.ContractStartDate,
                ContractType = systemACustomer.ContractType,
            };
            if(systemACustomer.name != systemBCustomer.name)
            {
                string newerSource = systemACustomer.last_updated > systemBCustomer.last_updated ? "SystemA" : "SystemB";
                mergedCustomerRecord.name = newerSource == "SystemA" ? systemACustomer.name : systemBCustomer.name;
                mergedCustomerRecord.Metadata = mergedCustomerRecord.Metadata ?? new MetaData();
                mergedCustomerRecord.Metadata.conflicts = mergedCustomerRecord.Metadata.conflicts ?? new List<Dictionary<string, string>>();
                mergedCustomerRecord.Metadata.conflicts.Add(new Dictionary<string, string> { {"field", "name" }, { "source", newerSource} });

            }
            if(systemACustomer.address != systemBCustomer.address)
            {
                mergedCustomerRecord.address =  systemBCustomer.address; // Assuming SystemB is the source of truth for address conflicts
                mergedCustomerRecord.Metadata = mergedCustomerRecord.Metadata ?? new MetaData();
                mergedCustomerRecord.Metadata.conflicts = mergedCustomerRecord.Metadata.conflicts ?? new List<Dictionary<string, string>>();
                mergedCustomerRecord.Metadata.conflicts.Add(new Dictionary<string, string> { {"field", "address" }, { "source", "SystemB"} });
            }

            return mergedCustomerRecord;
        }
    }
}