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



-- test CDC by inserting the records
INSERT INTO public.products(id,name,price,stock_quantity,created_at)
VALUEs(8,'test',12,12,'12-12-2025');

INSERT INTO public.customers(id,name,email,created_at)
VALUES(2,'John Doe','john.doe@noexist.com','12-12-2025');

INSERT INTO public.orders(id,customer_id,total_amount,status,created_at)
VALUES(1,2,10.10,1,'12-12-2025')

