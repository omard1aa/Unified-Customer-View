# ADR-01: Unified view architecture
## Status
Proposed

## Context
The company stores customer data across two independent systems:
System A – A legacy internal database that stores contract-related data.
System B – A newer external platform accessed through an HTTP API that stores contact information such as phone numbers and updated addresses. While System B's latency can be mitigated through caching, its potential unavailability is the primary architectural constraint, as it can lead to inaccurate or incomplete data being served to the customer.
There is currently no single source of truth for customer records. The same customer may appear in both systems with inconsistent or partially overlapping data for example different addresses or name spelling variations like "Muller" vs "Mueller." Each system is authoritative for different fields, so neither can be discarded entirely. A simple "pick the newer record" strategy doesn't work because the merge must happen at the field level, not the record level.

## Decision
### Trade-offs
Option 1: Materialized view
- Advantages:
  - Simpler and has relatively faster response as we access a storage service that has the data ready to be served without processing any logic
  - No dependency over both systems, no latency problems
- Disadvantages:
  - Data inconsistency, records on both systems can be updated anytime
  - Require additional storage layer
  - Conflict logic implementation can be more tricky and complex, for example in case of updating unmatched record to be matched if there is already metadata and fields indicates where the conflict issue happened we need to remove it, the conflicts already gone
  - Future improvements or going back to another solution (undo) has a relatively higher cost in terms of time and complexity, we cannot add we need to destroy it and re-build, in contrast with cache-aside we can add or enhance on top of it, or neglect the implementation if needed

Option 2: Cache aside
- Advantages:
  - Higher consistency, no layers in between the customer and both systems
  - Simpler in terms of going back to another solution
  - Simple conflict logic implementation
- Disadvantages:
  - Higher rate of executions, costs more computationally
  - Asynchronous or parallel programming more complex but manageable
  - Relatively slower, direct communication to both systems every request

### Data Strategy
Cache-aside is preferable as the data consistency is the highest priority for a customer profile service. The conflict logic implementation will be more accurate and stronger. The lightweight customer record and the small dataset (hundreds not millions) will lead to an acceptable performance vs outdated data response using the materialized view. Caching mitigates System B's latency for frequently accessed records. And the approach keeps the architecture simple enough to extend with event-driven sync or CQRS in the future without requiring a rebuild.

### High level architecture
#### Architecture overview
Client
   |
   v
Merge Service
   |-- Caching layer
   |
   |--- System A Repository (SQLite)
   |
   |--- System B Mock API (Async HTTP Call)
   |
   v
   ResolveConflict service
   |
   v
Unified Customer Response + Metadata

#### Diagram
flowchart TD

Client[Client Application]

API[Customer Merge Service API]

Cache[Cache Layer<br>Redis / In-Memory]

ARepo[System A Repository<br>SQLite Database]

BClient[System B Client<br>Async HTTP API]

MergeEngine[Merge Engine<br>Entity Resolution + Field Priority]

Metadata[Metadata Generator<br>Source Tracking + Conflict Detection]

Response[Unified Customer Response]

DiffEngine[Conflict Diff Engine<br>/customer/sync]

%% Normal GET flow
Client --> API
API --> Cache

Cache -- Cache Hit --> Response
Response --> Client

Cache -- Cache Miss --> ARepo
Cache -- Cache Miss --> BClient

ARepo --> MergeEngine
BClient --> MergeEngine

%% System B failure path
BClient -. timeout / unavailable .-> MergeEngine

MergeEngine --> Metadata
Metadata --> Response
Metadata --> Cache

%% Sync endpoint (bypasses cache)
API --> DiffEngine
DiffEngine --> ARepo
DiffEngine --> BClient

### Entity Resolution Strategy

Customers are matched using exact email matching, which is the unique identifier shared across both systems.

Sources of truth:
Field	 -                Source of Truth
phone:	                  System B
address:	              System B
contract_start_date:	  System A
contract_type:	          System A

Merge rules:
If a field exists in only one system, include it.
If a field exists in both systems, use the source-of-truth priority.
If values differ, mark the field as a conflict in _metadata.
Each field in the unified response includes provenance metadata indicating the originating system.

Example metadata structure:
"_metadata": {
  "sources": {
    "address": "SystemB",
    "contract_type": "SystemA"
  },
  "conflicts": ["address"]
}

### Failure handling
System B timeout: System A data only, marked as partial in metadata.
System B completely down: same as timeout but triggered by connection failure.
Missing data on both systems: will return only the data exists 