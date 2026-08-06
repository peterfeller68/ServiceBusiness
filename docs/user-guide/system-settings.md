# System Settings

Last reviewed: 2026-08-06

System Settings control deployment-level behavior such as Pool or Landscape mode, DevTest mode, and Independent Home Owner trial length.

## Opening Settings

System Administrators can open Settings / System Settings to view and edit:

- SystemMode
- DevTest
- HomeOwnerTrialDays

Configuration values can still provide startup defaults, but saved System Settings in storage are the ongoing source of truth after the settings row exists.

## SystemMode

Pool mode uses PoolShark branding and Pool Equipment features.

Landscape mode uses TreeShark branding and hides Pool Equipment features.

## DevTest

DevTest mode enables test sign-in and Test navigation. It should be enabled only in development or testing environments.

## HomeOwnerTrialDays

HomeOwnerTrialDays controls the trial length assigned to new Independent Home Owner subscriptions. Change it from Settings / System Settings when a different default trial length is needed.

The saved value is used for new registrations. Existing subscriptions keep their current trial end date unless a System Administrator changes that user directly from user management.
