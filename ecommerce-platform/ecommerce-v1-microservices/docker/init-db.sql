-- Create all service databases
CREATE DATABASE identity_db;
CREATE DATABASE product_db;
CREATE DATABASE order_db;
CREATE DATABASE coupon_db;
CREATE DATABASE payment_db;

GRANT ALL PRIVILEGES ON DATABASE identity_db  TO ecommerce;
GRANT ALL PRIVILEGES ON DATABASE product_db   TO ecommerce;
GRANT ALL PRIVILEGES ON DATABASE order_db     TO ecommerce;
GRANT ALL PRIVILEGES ON DATABASE coupon_db    TO ecommerce;
GRANT ALL PRIVILEGES ON DATABASE payment_db   TO ecommerce;
