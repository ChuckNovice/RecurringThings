-- RecurringThings PostgreSQL Schema
-- Version: 1.0.0
-- This migration creates the initial database schema for the RecurringThings library.

-- Schema version tracking table
CREATE TABLE IF NOT EXISTS __recurring_things_schema (
    version INTEGER NOT NULL PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- RECURRENCES TABLE
-- Stores recurring event patterns that generate virtualized occurrences
-- ============================================================================
CREATE TABLE IF NOT EXISTS recurrences (
    -- Primary key
    id UUID PRIMARY KEY,

    -- Multi-tenant isolation
    organization VARCHAR(100) NOT NULL,
    resource_path VARCHAR(100) NOT NULL,

    -- Event classification
    type VARCHAR(100) NOT NULL,

    -- Timing (all in UTC)
    start_time TIMESTAMPTZ NOT NULL,
    duration INTERVAL NOT NULL,
    recurrence_end_time TIMESTAMPTZ NOT NULL,

    -- RFC 5545 recurrence rule
    r_rule VARCHAR(2000) NOT NULL,

    -- IANA timezone identifier
    time_zone VARCHAR(100) NOT NULL,

    -- User-defined metadata
    extensions JSONB
);

-- Index for range queries: find recurrences overlapping a date range
-- Query pattern: organization, resource_path, type (optional), start_time <= end, recurrence_end_time >= start
CREATE INDEX IF NOT EXISTS idx_recurrences_query
    ON recurrences (organization, resource_path, type, start_time, recurrence_end_time);

-- ============================================================================
-- OCCURRENCES TABLE
-- Stores standalone (non-recurring) events
-- ============================================================================
CREATE TABLE IF NOT EXISTS occurrences (
    -- Primary key
    id UUID PRIMARY KEY,

    -- Multi-tenant isolation
    organization VARCHAR(100) NOT NULL,
    resource_path VARCHAR(100) NOT NULL,

    -- Event classification
    type VARCHAR(100) NOT NULL,

    -- Timing (all in UTC)
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    duration INTERVAL NOT NULL,

    -- IANA timezone identifier
    time_zone VARCHAR(100) NOT NULL,

    -- User-defined metadata
    extensions JSONB
);

-- Index for range queries: find occurrences overlapping a date range
-- Query pattern: organization, resource_path, type (optional), start_time <= end, end_time >= start
CREATE INDEX IF NOT EXISTS idx_occurrences_query
    ON occurrences (organization, resource_path, type, start_time, end_time);

-- ============================================================================
-- OCCURRENCE EXCEPTIONS TABLE
-- Stores cancellations of specific virtualized occurrences from recurrences
-- ============================================================================
CREATE TABLE IF NOT EXISTS occurrence_exceptions (
    -- Primary key
    id UUID PRIMARY KEY,

    -- Multi-tenant isolation (must match parent recurrence)
    organization VARCHAR(100) NOT NULL,
    resource_path VARCHAR(100) NOT NULL,

    -- Parent recurrence reference with cascade delete
    recurrence_id UUID NOT NULL REFERENCES recurrences(id) ON DELETE CASCADE,

    -- Original occurrence time being cancelled (UTC)
    original_time_utc TIMESTAMPTZ NOT NULL,

    -- User-defined metadata
    extensions JSONB
);

-- Index for finding exceptions by recurrence
CREATE INDEX IF NOT EXISTS idx_exceptions_recurrence
    ON occurrence_exceptions (recurrence_id);

-- Index for range queries during virtualization
CREATE INDEX IF NOT EXISTS idx_exceptions_query
    ON occurrence_exceptions (organization, resource_path, original_time_utc);

-- ============================================================================
-- OCCURRENCE OVERRIDES TABLE
-- Stores modifications to specific virtualized occurrences from recurrences
-- ============================================================================
CREATE TABLE IF NOT EXISTS occurrence_overrides (
    -- Primary key
    id UUID PRIMARY KEY,

    -- Multi-tenant isolation (must match parent recurrence)
    organization VARCHAR(100) NOT NULL,
    resource_path VARCHAR(100) NOT NULL,

    -- Parent recurrence reference with cascade delete
    recurrence_id UUID NOT NULL REFERENCES recurrences(id) ON DELETE CASCADE,

    -- Original occurrence time being modified (UTC)
    original_time_utc TIMESTAMPTZ NOT NULL,

    -- New timing values (UTC)
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    duration INTERVAL NOT NULL,

    -- Denormalized original values (for showing "before and after")
    original_duration INTERVAL NOT NULL,
    original_extensions JSONB,

    -- User-defined metadata
    extensions JSONB
);

-- Index for finding overrides by recurrence
CREATE INDEX IF NOT EXISTS idx_overrides_recurrence
    ON occurrence_overrides (recurrence_id);

-- Index for finding overrides by original time
CREATE INDEX IF NOT EXISTS idx_overrides_original
    ON occurrence_overrides (organization, resource_path, original_time_utc);

-- Index for finding overrides by actual time range
CREATE INDEX IF NOT EXISTS idx_overrides_start
    ON occurrence_overrides (organization, resource_path, start_time, end_time);

-- Record schema version
INSERT INTO __recurring_things_schema (version) VALUES (1)
ON CONFLICT (version) DO NOTHING;
