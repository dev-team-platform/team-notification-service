ALTER TABLE notifications
ADD COLUMN source_app VARCHAR(100);

UPDATE notifications
SET source_app = 'team-organization'
WHERE source_app IS NULL;

ALTER TABLE notifications
ALTER COLUMN source_app SET NOT NULL;

CREATE INDEX ix_notifications_source_app_created_at
ON notifications (source_app, created_at DESC);
