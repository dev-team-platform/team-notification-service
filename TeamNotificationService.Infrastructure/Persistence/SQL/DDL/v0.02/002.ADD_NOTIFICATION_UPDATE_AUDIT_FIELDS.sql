ALTER TABLE notifications
ADD COLUMN updated_at TIMESTAMPTZ NULL,
ADD COLUMN updated_by_id UUID NULL;
