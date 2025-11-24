-- DoorX Database Initialization Script
-- This script runs automatically when the PostgreSQL container starts for the first time

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schemas (if using schema separation)
-- CREATE SCHEMA IF NOT EXISTS doorx;

-- Create initial tables will be done by Entity Framework migrations
-- This file is for setting up extensions, initial data, etc.

-- Example: Create a health check function
CREATE OR REPLACE FUNCTION public.health_check()
RETURNS TABLE(status text, timestamp timestamptz) AS $$
BEGIN
    RETURN QUERY SELECT 'healthy'::text, now();
END;
$$ LANGUAGE plpgsql;

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'DoorX database initialized successfully at %', now();
END $$;
