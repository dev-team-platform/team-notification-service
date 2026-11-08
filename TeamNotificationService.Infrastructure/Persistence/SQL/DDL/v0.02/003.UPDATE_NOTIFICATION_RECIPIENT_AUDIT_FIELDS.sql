ALTER TABLE notification_recipients
DROP COLUMN is_archived;

ALTER TABLE notification_recipients
RENAME COLUMN archived_at TO updated_at;
