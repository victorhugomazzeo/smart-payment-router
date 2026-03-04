# Smart Payment Router

A microservices-based payment processor selector that intelligently 
routes transactions to the best processor based on BIN, card brand, 
installments, and historical approval rates.

## Overview

Smart Payment Router selects the optimal payment processor for each 
transaction by analyzing historical data and approval rates. The system 
learns from transaction results to continuously improve routing decisions.

## Architecture

- Selector Service (.NET Core): Real-time processor selection
- Analytics Service (.NET Core): Daily batch processing and learning
- Message Broker: RabbitMQ for event-driven communication
- Databases: PostgreSQL, MongoDB, Redis

## Key Features

- Sub-100ms routing decisions
- Machine learning integration (roadmap)
- Real-time metric updates
- Event-driven architecture
- Docker Compose setup

## Use Case

E-commerce platforms, payment gateways, and subscription services can 
integrate this API to minimize payment rejections and reduce transaction 
costs by automatically selecting the best processor for each card/amount/installment combination.

## Tech Stack

- C# .NET Core
- PostgreSQL
- MongoDB
- Redis
- RabbitMQ
- Docker

## How It Works

1. Client sends transaction parameters (BIN, brand, installments)
2. Selector returns optimal processor in <100ms
3. Transaction is processed with chosen processor
4. Result arrives and Analytics processes it (1x daily)
5. Approval rates are recalculated
6. Selector updates its decisions with new data

## Future Enhancements

- Bayesian inference for decision making
- Advanced ML models
- Real-time processing
- Data lake integration
