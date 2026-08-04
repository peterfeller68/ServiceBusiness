# 1. Logs
The Logs Menu will be at the Level of the Settings Menu after Report

## 1.1 Email Log
Show EmailLogEntry entities
The top row shall include panels with counts by Status
The second row shall be a panel with the Detailed Info
Add the ability to filter by a Date
Columns: From, To, cc, Subject, Status, SendDate, FailedMessage
Actions: View (icon)
The view action witll show the Body

### 1.1.1 Sys Admin
Show only entities for the service (Pool or Landscape)
Columns: Service Client, From, To, Subject, Status, SendDate, FailedMessage
Allow all actions 

### 1.1.2 Business Owner
Show only entities for the service client
Columns: From, To, Subject, Status, SendDate, FailedMessage
Allow all actions 

### 1.1.3 Business Employee
No Access

### 1.1.4 Business Client
Show only entities for messages that were sent to him
Columns: From, To, Subject, Status, SendDate, FailedMessage
Allow all actions 

### 1.1.5 Independent Home Owner
Show only entities for messages that were sent to him
Columns: From, To, Subject, Status, SendDate, FailedMessage
Allow all actions 

Current implementation:
- The Logs navigation group appears after Reports for System Administrators, Business Owners, Business Clients, and Independent Home Owners; Business Employees do not receive Logs access.
- Email Log is available at `/logs/email`, with the legacy `/admin/email-log` route retained for System Administrators.
- `EmailLogService` centralizes visibility rules: System Administrators see platform logs and service-client logs for the active Pool or Landscape system mode, Business Owners see logs for their active service client, Business Clients and Independent Home Owners see only messages addressed to them, and Business Employees receive no log rows.
- The Email Log page shows status-count panels, an optional date filter, a collapsible detail panel, From/To/CC/Subject/Status/SendDate/FailedMessage columns, a Service Client column for System Administrators, and a View icon action that displays the message body.
