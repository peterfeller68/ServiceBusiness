# Roles and Permissions

Last reviewed: 2026-08-04

System Administrators use Roles to manage the labels and permission metadata for built-in company roles.

## Opening Roles

Open System Administrator / User Roles.

The Roles page lists:

- Role ID
- Name
- Description
- Owner Approval
- Permission count

## Editing A Role

1. Select Edit for a role.
2. Update the display name, description, owner approval setting, or permissions.
3. Save the role.

The role ID cannot be changed. Permissions can be entered on separate lines or separated by commas.

## Current Limitation

Role identities are fixed to Business Owner, Business Employee, and Business Client equivalents. Permission strings are stored as metadata today; runtime access is still enforced by the user's system-admin flag, active company role, and tenant/client scope.
