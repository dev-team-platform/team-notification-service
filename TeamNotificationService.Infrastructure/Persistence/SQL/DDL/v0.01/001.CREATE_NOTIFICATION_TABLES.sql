CREATE TABLE notifications (
    id UUID PRIMARY KEY,
    title VARCHAR(255) NULL,
    content TEXT NULL,
    data JSONB NULL,
    created_by_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX ix_notifications_created_at
ON notifications (created_at DESC);

CREATE INDEX ix_notifications_created_by_id
ON notifications (created_by_id);


CREATE TABLE notification_recipients (
    id UUID PRIMARY KEY,
    notification_id UUID NOT NULL,
    user_id UUID NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMPTZ NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE notification_recipients
ADD CONSTRAINT fk_notification_recipients_notification_id
FOREIGN KEY (notification_id)
REFERENCES notifications (id);

ALTER TABLE notification_recipients
ADD CONSTRAINT uq_notification_recipients_user_id_notification_id
UNIQUE (user_id, notification_id);

CREATE INDEX ix_notification_recipients_user_id_created_at
ON notification_recipients (user_id, created_at DESC);

CREATE INDEX ix_notification_recipients_user_id_is_read_created_at
ON notification_recipients (user_id, is_read, created_at DESC);


CREATE TABLE notification_deliveries (
    id UUID PRIMARY KEY,
    notification_recipient_id UUID NOT NULL,
    channel VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL,
    destination VARCHAR(255) NULL,
    template_key VARCHAR(150) NULL,
    sent_at TIMESTAMPTZ NULL,
    delivered_at TIMESTAMPTZ NULL,
    failed_at TIMESTAMPTZ NULL,
    retry_count INT NOT NULL DEFAULT 0,
    last_error TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NULL
);

ALTER TABLE notification_deliveries
ADD CONSTRAINT fk_notification_deliveries_notification_recipient_id
FOREIGN KEY (notification_recipient_id)
REFERENCES notification_recipients (id);


CREATE TABLE email_templates (
    id UUID PRIMARY KEY,
    template_key VARCHAR(255) NOT NULL,
    subject_template VARCHAR(500) NOT NULL,
    body_template TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NULL
);

ALTER TABLE email_templates
ADD CONSTRAINT uq_email_templates_template_key
UNIQUE (template_key);

CREATE INDEX ix_notification_deliveries_recipient_id
ON notification_deliveries (notification_recipient_id);

CREATE UNIQUE INDEX uq_notification_deliveries_recipient_channel
ON notification_deliveries (notification_recipient_id, channel);
