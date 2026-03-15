# Unified Customer View Service

## Overview
Customer data currently exists across two separate systems: a legacy internal database (System A) and a newer external platform (System B). This can lead to data inconsistency and difficulty retrieving a reliable customer data.

To address this, we introduce a Merge Service that aggregates and combine data from both systems. The service retrieves records using email-based matching, evaluates conflicts using field-level trust priorities, and produces a single unified customer response.

The system also includes conflict detection with provenance tracking to record the origin of each field.

## Architecture
[Brief summary + link to the ADR document for details]

## Tech Stack
- .Net Core: Merge service 
- SQLite: System A
- Redis: Caching layer 
- Docker: 1 starting point for the 3 systems

## Project Structure
- /src
  - /SystemA
  - /SystemB
  - /MergeService 
    - /Controllers
    - /Models
    - /Interfaces
    - /Repositories
    - /Services (Logic)
    - /Middleware
    - /DTOs
    - /Filters
- /tests
- /docs
- docker-compose.yml
- README.md

## Prerequisites
- Docker & docker compose up

## Getting Started
  `docker compose up`. the service runs on https://localhost:5000
- A Postman collection is included at /docs/UnifiedView.json

## API Endpoints
- GET /customer/{email} - Returns a unified customer record by exact email match.
- GET /customer/search?q={query} - Search across both systems.
- POST /customer/sync - Returns the difference in one record between the two sources
- GET /health -  check connectivity health of both data sources