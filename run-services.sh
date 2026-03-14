#!/bin/bash
set -e

# Ports to clean
ports=(5000 5001 5002)

echo "Checking ports..."

for port in "${ports[@]}"
do
  pid=$(lsof -ti:$port)

  if [ -n "$pid" ]; then
    echo "Port $port is in use by PID $pid. Killing..."
    kill -9 $pid
  else
    echo "Port $port is free."
  fi
done

echo "Building services..."

dotnet build ./src/SystemA
dotnet build ./src/SystemB
dotnet build ./src/MergeService

echo "Starting services..."

dotnet run --project ./src/SystemA &
dotnet run --project ./src/SystemB &
dotnet run --project ./src/MergeService &

wait