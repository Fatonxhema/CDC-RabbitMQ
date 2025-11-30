#!/bin/bash

echo "Building CDC Pipeline Docker images..."

# Build Consumer API
docker build -t cdc-consumer-api:latest -f src/CDC.Consumer.API/Dockerfile .

# Build Listener API
docker build -t cdc-listener-api:latest -f src/CDC.Listener.API/Dockerfile .

# Build Retry Processor
docker build -t cdc-retry-processor:latest -f src/CDC.RetryProcessor/Dockerfile .

echo "Build complete!"
