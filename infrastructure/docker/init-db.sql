-- Enable logical replication
ALTER SYSTEM SET wal_level = 'logical';

-- Create sample tables
CREATE TABLE IF NOT EXISTS customers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS orders (
    id SERIAL PRIMARY KEY,
    customer_id INTEGER REFERENCES customers(id),
    total_amount DECIMAL(10, 2) NOT NULL,
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    stock_quantity INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE PUBLICATION dbz_publication FOR ALL TABLES;

-- Insert sample routing configurations
INSERT INTO routing_configurations (id, table_name, exchange, routing_key, queue, is_active, created_at)
VALUES 
    (gen_random_uuid(), 'customers', 'cdc.exchange', 'customer.events', 'customer.events', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'orders', 'cdc.exchange', 'order.events', 'order.events', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'products', 'cdc.exchange', 'product.events', 'product.events', true, CURRENT_TIMESTAMP);

