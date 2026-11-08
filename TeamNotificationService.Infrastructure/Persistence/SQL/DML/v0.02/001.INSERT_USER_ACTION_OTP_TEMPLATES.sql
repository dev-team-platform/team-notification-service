BEGIN;

INSERT INTO email_templates (
    id,
    template_key,
    subject_template,
    body_template,
    is_active
)
VALUES
(
    '6c046844-6a14-4df2-b582-4b26aef76a81'::uuid,
    'ChangeRole',
    'Confirm role change for {{targetDisplayName}}',
    $change_role_html$
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="color-scheme" content="light" />
    <title>Team Organization security verification</title>
    <style>
      body,
      table,
      td,
      a {
        -webkit-text-size-adjust: 100%;
        -ms-text-size-adjust: 100%;
      }

      img {
        -ms-interpolation-mode: bicubic;
        border: 0;
        outline: none;
        text-decoration: none;
      }

      @media screen and (max-width: 620px) {
        .email-wrap {
          width: 100% !important;
        }

        .email-padding {
          padding-left: 22px !important;
          padding-right: 22px !important;
        }

        .otp-code {
          font-size: 30px !important;
          letter-spacing: 8px !important;
        }
      }
    </style>
  </head>
  <body
    style="
      margin: 0;
      padding: 0;
      background-color: #f3f4f6;
      font-family: Arial, Helvetica, sans-serif;
      color: #1f2937;
    "
  >
    <div
      style="
        display: none;
        max-height: 0;
        overflow: hidden;
        opacity: 0;
        color: transparent;
      "
    >
      Your Team Organization one-time verification code is {{otpCode}}.
    </div>

    <table
      role="presentation"
      width="100%"
      cellspacing="0"
      cellpadding="0"
      border="0"
      style="width: 100%; background-color: #f3f4f6"
    >
      <tr>
        <td align="center" style="padding: 36px 14px">
          <table
            role="presentation"
            class="email-wrap"
            width="600"
            cellspacing="0"
            cellpadding="0"
            border="0"
            style="
              width: 600px;
              max-width: 600px;
              border-collapse: separate;
              background-color: #ffffff;
              border: 1px solid #e5e7eb;
              border-radius: 12px;
              overflow: hidden;
            "
          >
            <tr>
              <td style="padding: 23px 34px; background-color: #1d4ed8">
                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                >
                  <tr>
                    <td
                      align="left"
                      style="
                        color: #ffffff;
                        font-size: 18px;
                        line-height: 24px;
                        font-weight: 700;
                        letter-spacing: 0.2px;
                      "
                    >
                      <span style="vertical-align: middle"
                        >TEAM ORGANIZATION</span
                      >
                    </td>
                    <td
                      align="right"
                      style="
                        color: #dbeafe;
                        font-size: 12px;
                        line-height: 18px;
                        font-weight: 600;
                        letter-spacing: 0.7px;
                        text-transform: uppercase;
                      "
                    >
                      Security verification
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            <tr>
              <td class="email-padding" style="padding: 36px 42px 38px">
                <div
                  style="
                    margin-bottom: 14px;
                    color: #2563eb;
                    font-size: 11px;
                    line-height: 16px;
                    font-weight: 700;
                    letter-spacing: 1.1px;
                    text-transform: uppercase;
                  "
                >
                  Security verification&nbsp;&nbsp;·&nbsp;&nbsp;Role change
                </div>
                <h1
                  style="
                    margin: 0 0 15px;
                    color: #111827;
                    font-size: 26px;
                    line-height: 34px;
                    font-weight: 700;
                  "
                >
                  Confirm role change
                </h1>
                <p
                  style="
                    margin: 0 0 12px;
                    color: #374151;
                    font-size: 15px;
                    line-height: 24px;
                  "
                >
                  Hello {{recipientDisplayName}},
                </p>
                <p
                  style="
                    margin: 0 0 23px;
                    color: #4b5563;
                    font-size: 15px;
                    line-height: 24px;
                  "
                >
                  A request was made to update a user’s role in Team
                  Organization. Confirm the change using the one-time password
                  below.
                </p>

                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                  style="
                    margin: 0 0 24px;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                    background-color: #f9fafb;
                  "
                >
                  <tr>
                    <td
                      style="
                        padding: 17px 18px 8px;
                        color: #6b7280;
                        font-size: 12px;
                        line-height: 17px;
                      "
                    >
                      USER
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 0 18px 14px;
                        color: #1f2937;
                        font-size: 14px;
                        line-height: 20px;
                        font-weight: 700;
                      "
                    >
                      {{targetDisplayName}}
                      <span style="color: #6b7280; font-weight: 400"
                        >({{targetUsername}})</span
                      >
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 12px 18px 8px;
                        border-top: 1px solid #e5e7eb;
                        color: #6b7280;
                        font-size: 12px;
                        line-height: 17px;
                      "
                    >
                      ROLE CHANGE
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 0 18px 17px;
                        color: #1f2937;
                        font-size: 14px;
                        line-height: 20px;
                        font-weight: 600;
                      "
                    >
                      <span style="color: #6b7280; font-weight: 400"
                        >{{currentRole}}</span
                      >
                      <span style="padding: 0 9px; color: #9ca3af"
                        >&#8594;</span
                      >
                      <span style="color: #1d4ed8">{{newRole}}</span>
                    </td>
                  </tr>
                </table>

                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                  style="
                    margin: 0 0 21px;
                    border-radius: 8px;
                    background-color: #eff6ff;
                  "
                >
                  <tr>
                    <td align="center" style="padding: 19px 18px 17px">
                      <div
                        style="
                          margin-bottom: 8px;
                          color: #4b5563;
                          font-size: 12px;
                          line-height: 17px;
                        "
                      >
                        YOUR ONE-TIME PASSWORD
                      </div>
                      <div
                        class="otp-code"
                        style="
                          color: #1d4ed8;
                          font-size: 34px;
                          line-height: 42px;
                          font-weight: 700;
                          letter-spacing: 10px;
                        "
                      >
                        {{otpCode}}
                      </div>
                      <div
                        style="
                          margin-top: 8px;
                          color: #4b5563;
                          font-size: 12px;
                          line-height: 18px;
                          font-weight: 700;
                        "
                      >
                        <strong>Expires in {{otpExpiryMinutes}} minutes</strong>
                      </div>
                    </td>
                  </tr>
                </table>

                <p
                  style="
                    margin: 0 0 22px;
                    color: #6b7280;
                    font-size: 13px;
                    line-height: 20px;
                  "
                >
                  Enter this code in Team Organization to confirm the role
                  change. For your security, do not share this code with anyone.
                </p>
                <p
                  style="
                    margin: 0;
                    color: #6b7280;
                    font-size: 13px;
                    line-height: 20px;
                  "
                >
                  If you did not initiate this request, ignore this email and
                  contact
                  <a
                    href="mailto:it-support@team-platform.com"
                    style="color: #1d4ed8; text-decoration: underline"
                    >it-support@team-platform.com</a
                  >.
                </p>
              </td>
            </tr>

            <tr>
              <td
                style="
                  padding: 20px 34px;
                  border-top: 1px solid #e5e7eb;
                  background-color: #f9fafb;
                "
              >
                <p
                  style="
                    margin: 0 0 6px;
                    color: #6b7280;
                    font-size: 12px;
                    line-height: 18px;
                  "
                >
                  This is an automated security message from Team Organization.
                  Replies to this email are not monitored.
                </p>
                <p
                  style="
                    margin: 0;
                    color: #9ca3af;
                    font-size: 11px;
                    line-height: 17px;
                  "
                >
                  &copy; {{currentYear}} Team Organization
                </p>
              </td>
            </tr>
          </table>

          <p
            style="
              margin: 16px 0 0;
              color: #9ca3af;
              font-size: 11px;
              line-height: 16px;
              text-align: center;
            "
          >
            This message contains a one-time password. Keep it private.
          </p>
        </td>
      </tr>
    </table>
  </body>
</html>
$change_role_html$,
    TRUE
),
(
    '851824e9-7c61-4eac-8c2d-bf6869532f11'::uuid,
    'DisableUser',
    'Confirm account disablement for {{targetDisplayName}}',
    $disable_user_html$
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="color-scheme" content="light" />
    <title>Team Organization security verification</title>
    <style>
      body,
      table,
      td,
      a {
        -webkit-text-size-adjust: 100%;
        -ms-text-size-adjust: 100%;
      }

      img {
        -ms-interpolation-mode: bicubic;
        border: 0;
        outline: none;
        text-decoration: none;
      }

      @media screen and (max-width: 620px) {
        .email-wrap {
          width: 100% !important;
        }

        .email-padding {
          padding-left: 22px !important;
          padding-right: 22px !important;
        }

        .otp-code {
          font-size: 30px !important;
          letter-spacing: 8px !important;
        }
      }
    </style>
  </head>
  <body
    style="
      margin: 0;
      padding: 0;
      background-color: #f3f4f6;
      font-family: Arial, Helvetica, sans-serif;
      color: #1f2937;
    "
  >
    <div
      style="
        display: none;
        max-height: 0;
        overflow: hidden;
        opacity: 0;
        color: transparent;
      "
    >
      Your Team Organization one-time verification code is {{otpCode}}.
    </div>

    <table
      role="presentation"
      width="100%"
      cellspacing="0"
      cellpadding="0"
      border="0"
      style="width: 100%; background-color: #f3f4f6"
    >
      <tr>
        <td align="center" style="padding: 36px 14px">
          <table
            role="presentation"
            class="email-wrap"
            width="600"
            cellspacing="0"
            cellpadding="0"
            border="0"
            style="
              width: 600px;
              max-width: 600px;
              border-collapse: separate;
              background-color: #ffffff;
              border: 1px solid #e5e7eb;
              border-radius: 12px;
              overflow: hidden;
            "
          >
            <tr>
              <td style="padding: 23px 34px; background-color: #1d4ed8">
                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                >
                  <tr>
                    <td
                      align="left"
                      style="
                        color: #ffffff;
                        font-size: 18px;
                        line-height: 24px;
                        font-weight: 700;
                        letter-spacing: 0.2px;
                      "
                    >
                      <span style="vertical-align: middle"
                        >TEAM ORGANIZATION</span
                      >
                    </td>
                    <td
                      align="right"
                      style="
                        color: #dbeafe;
                        font-size: 12px;
                        line-height: 18px;
                        font-weight: 600;
                        letter-spacing: 0.7px;
                        text-transform: uppercase;
                      "
                    >
                      Security verification
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            <tr>
              <td class="email-padding" style="padding: 36px 42px 38px">
                <div
                  style="
                    margin-bottom: 14px;
                    color: #2563eb;
                    font-size: 11px;
                    line-height: 16px;
                    font-weight: 700;
                    letter-spacing: 1.1px;
                    text-transform: uppercase;
                  "
                >
                  Security verification&nbsp;&nbsp;·&nbsp;&nbsp;User disablement
                </div>
                <h1
                  style="
                    margin: 0 0 15px;
                    color: #111827;
                    font-size: 26px;
                    line-height: 34px;
                    font-weight: 700;
                  "
                >
                  Confirm account disablement
                </h1>
                <p
                  style="
                    margin: 0 0 12px;
                    color: #374151;
                    font-size: 15px;
                    line-height: 24px;
                  "
                >
                  Hello {{recipientDisplayName}},
                </p>
                <p
                  style="
                    margin: 0 0 23px;
                    color: #4b5563;
                    font-size: 15px;
                    line-height: 24px;
                  "
                >
                  A request was made to disable a user account in Team
                  Organization. Confirm the action using the one-time password
                  below.
                </p>

                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                  style="
                    margin: 0 0 24px;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                    background-color: #f9fafb;
                  "
                >
                  <tr>
                    <td
                      style="
                        padding: 17px 18px 8px;
                        color: #6b7280;
                        font-size: 12px;
                        line-height: 17px;
                      "
                    >
                      USER
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 0 18px 14px;
                        color: #1f2937;
                        font-size: 14px;
                        line-height: 20px;
                        font-weight: 700;
                      "
                    >
                      {{targetDisplayName}}
                      <span style="color: #6b7280; font-weight: 400"
                        >({{targetUsername}})</span
                      >
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 12px 18px 8px;
                        border-top: 1px solid #e5e7eb;
                        color: #6b7280;
                        font-size: 12px;
                        line-height: 17px;
                      "
                    >
                      ACCOUNT STATUS
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="
                        padding: 0 18px 17px;
                        color: #1f2937;
                        font-size: 14px;
                        line-height: 20px;
                        font-weight: 600;
                      "
                    >
                      <span style="color: #6b7280; font-weight: 400"
                        >{{currentStatus}}</span
                      >
                      <span style="padding: 0 9px; color: #9ca3af"
                        >&#8594;</span
                      >
                      <span style="color: #1d4ed8">Disabled</span>
                    </td>
                  </tr>
                </table>

                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                  style="
                    margin: 0 0 21px;
                    border-radius: 8px;
                    background-color: #eff6ff;
                  "
                >
                  <tr>
                    <td align="center" style="padding: 19px 18px 17px">
                      <div
                        style="
                          margin-bottom: 8px;
                          color: #4b5563;
                          font-size: 12px;
                          line-height: 17px;
                        "
                      >
                        YOUR ONE-TIME PASSWORD
                      </div>
                      <div
                        class="otp-code"
                        style="
                          color: #1d4ed8;
                          font-size: 34px;
                          line-height: 42px;
                          font-weight: 700;
                          letter-spacing: 10px;
                        "
                      >
                        {{otpCode}}
                      </div>
                      <div
                        style="
                          margin-top: 8px;
                          color: #4b5563;
                          font-size: 12px;
                          line-height: 18px;
                          font-weight: 700;
                        "
                      >
                        <strong>Expires in {{otpExpiryMinutes}} minutes</strong>
                      </div>
                    </td>
                  </tr>
                </table>

                <p
                  style="
                    margin: 0 0 22px;
                    color: #6b7280;
                    font-size: 13px;
                    line-height: 20px;
                  "
                >
                  Enter this code in Team Organization to confirm account
                  disablement. For your security, do not share this code with
                  anyone.
                </p>
                <p
                  style="
                    margin: 0;
                    color: #6b7280;
                    font-size: 13px;
                    line-height: 20px;
                  "
                >
                  If you did not initiate this request, ignore this email and
                  contact
                  <a
                    href="mailto:it-support@team-platform.com"
                    style="color: #1d4ed8; text-decoration: underline"
                    >it-support@team-platform.com</a
                  >.
                </p>
              </td>
            </tr>

            <tr>
              <td
                style="
                  padding: 20px 34px;
                  border-top: 1px solid #e5e7eb;
                  background-color: #f9fafb;
                "
              >
                <p
                  style="
                    margin: 0 0 6px;
                    color: #6b7280;
                    font-size: 12px;
                    line-height: 18px;
                  "
                >
                  This is an automated security message from Team Organization.
                  Replies to this email are not monitored.
                </p>
                <p
                  style="
                    margin: 0;
                    color: #9ca3af;
                    font-size: 11px;
                    line-height: 17px;
                  "
                >
                  &copy; {{currentYear}} Team Organization
                </p>
              </td>
            </tr>
          </table>

          <p
            style="
              margin: 16px 0 0;
              color: #9ca3af;
              font-size: 11px;
              line-height: 16px;
              text-align: center;
            "
          >
            This message contains a one-time password. Keep it private.
          </p>
        </td>
      </tr>
    </table>
  </body>
</html>
$disable_user_html$,
    TRUE
)
ON CONFLICT (template_key) DO UPDATE
SET subject_template = EXCLUDED.subject_template,
    body_template = EXCLUDED.body_template,
    is_active = EXCLUDED.is_active,
    updated_at = CURRENT_TIMESTAMP;

COMMIT;
