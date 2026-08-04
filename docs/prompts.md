++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
follow-on implementation-prompt
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

# Codex Implementation Prompt

Use this prompt to continue enhancing the application after the initial vertical slice is complete. Follow the implementation priorities and technical constraints outlined in the prompt, and refer to the source documents for detailed requirements, architecture, storage entities, and UI specifications.

## Prompt

Source of Truth Documents
Use these documents as the source of truth, however you have already implemented them:
- `docs/requirements.md`
- `docs/architecture.md`
- `docs/storage-entities.md`
- `docs/ui-specifications.md`

Use this document as the source of truth for the follow-on implementation:
- `docs/follow-on requirements.md`

Go ahead and implement any changes since the last update.
Also, update the above source of truth documents with the neccessary changes in order to reflect the current state of the application and the new features that have been implemented.
This will ensure that the documentation remains accurate and up-to-date for future reference and development.


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
follow-on implementation-prompt with focus
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Use previous # Codex Implementation Prompt
Focus On:
## 1.10 Dashboard
### 11.1.1 Test Companies
### 11.1.2 Additional Test Users, not associated with any Company


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Miscellaneous
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Review section #12 and update the Current Implementation paragraph

Review section #14 and add the Current Implementation paragraph

Focus on section 14. Looking at section 14, what changes do you need to make to the current product?

Go ahead and implement the identified changes and update the Current Implementation paragraphs.

Go ahead and implement the updates to section 14 and update the Current Implementation paragraphs.

Go ahead and implement the new feature in section 15 and add the Current Implementation paragraphs.

Do not start the WebApp from here. I will run it via VS.

Create a check-in comment for all changes waiting to be committed, commit and push the changes.

Section 14.4 - The requirements state:
The users listed need to belong to the service. 
Don't allow the logged in user to change his/her Status, Approval Status or User Type
Issues:
It shows all users. pool and landscape
I am logged in as the sysadmin and I can edit my user

I want to get a comprehensive list of materials needed for a lanscaping business as a csv, divided into categories. The output should look like this:
Brand, Category, Name, Model No
Brand1, Citrus Tree Fertilizer, 1 lb Bag, 3532309-45 

I want to get a comprehensive list of services needed for a landscaping business as a csv, divided into categories. The output should look like this:
Category, Name, Description
Irrigation, Replace Nozzle, bla bla bla
Tree Maintenance, Cut Down Tree < 10ft, bla bla bla

++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven feature implementation
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Implement docs/features/<feature-name>.md using the spec-driven workflow. Keep the feature spec, user guide, 
source-of-truth docs, tests, Current Implementation, Outstanding Tasks, and Change Log up to date. 
Do not start the WebApp. Run tests, then commit and push with a check-in comment.

++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven feature implementation
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Implement the feature described in:

docs/features/<feature-name>.md

Follow our spec-driven workflow:

1. Review the feature spec, source-of-truth docs, and any related user-guide or operations docs.
2. Inspect the current implementation before making changes.
3. Implement the feature according to the spec.
4. Update the feature spec with:
   - Current Implementation
   - Outstanding Tasks
   - Acceptance Criteria status
   - Test coverage added or still missing
   - Change Log entry
5. Update user documentation under docs/user-guide/ if this changes user-visible behavior.
6. Update source-of-truth docs under docs/source-of-truth/ only if this changes global product requirements, architecture, roles/permissions, data model, storage, navigation, or UI rules.
7. Add or update tests for the implemented behavior.
8. Do not start the WebApp. I will run it via Visual Studio.
9. Run applicable tests.
WATCH OUT
10. Create a check-in comment, commit, and push the changes.

Before implementing, briefly summarize:
- What the feature requires
- Which docs you expect to update
- Which code areas you expect to touch
- Any ambiguity or risk you found
WATCH OUT
- Create a check-in comment, commit, and push the changes.


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven feature implementation II
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Implement the missing Acceptance Criteria described in:

docs/features/jobs.md

- Not implemented: A hosted scheduler or external worker automatically invokes the invoicing or email job services.

Follow our spec-driven workflow:

1. Review the feature spec, source-of-truth docs, and any related user-guide or operations docs.
2. Inspect the current implementation before making changes.
3. Implement the feature according to the spec.
4. Update the feature spec with:
   - Current Implementation
   - Outstanding Tasks
   - Acceptance Criteria status
   - Test coverage added or still missing
   - Change Log entry
5. Update user documentation under docs/user-guide/ if this changes user-visible behavior.
6. Update source-of-truth docs under docs/source-of-truth/ only if this changes global product requirements, architecture, roles/permissions, data model, storage, navigation, or UI rules.
7. Add or update tests for the implemented behavior.
8. Do not start the WebApp. I will run it via Visual Studio.
9. Run applicable tests.
WATCH OUT
10. Create a check-in comment, commit, and push the changes.

Before implementing, briefly summarize:
- What the feature requires
- Which docs you expect to update
- Which code areas you expect to touch
- Any ambiguity or risk you found


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven NEW feature implementation
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Create and implement a new feature spec for:

<feature name>

Use docs/features/000-template.md as the structure.

First, create docs/features/<feature-name>.md with:
- Problem
- Personas
- Requirements
- User Flows
- UI Expectations
- Data Model Impact
- Authorization Rules
- Acceptance Criteria
- Tests
- User Documentation Impact
- Current Implementation
- Outstanding Tasks
- Change Log

Then implement the feature.

Also:
- Update docs/user-guide/ for user-visible behavior.
- Update docs/source-of-truth/ only for global product, architecture, role, data, storage, navigation, or UI changes.
- Add or update tests.
- Do not start the WebApp. I will run it via Visual Studio.
- Run applicable tests.
WATCH OUT
- Create a check-in comment, commit, and push the changes.


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven Update Documentation for already implemented feature
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Bring the documentation current for the implemented invoicing feature.

Inspect the current code and tests, then update or create docs/features/invoicing.md, update docs/user-guide/ as needed, 
and update docs/source-of-truth/ only where the global requirements/data model/UI docs are stale. 
Capture Current Implementation, Acceptance Criteria status, Tests, Outstanding Tasks, and a Change Log entry. 
Do not start the WebApp.

++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Spec-driven Update Documentation for already implemented feature
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Bring the documentation current for the already-implemented feature:

<feature name or route/page/service area>

Please inspect the current code and tests first, then update documentation to match the actual implementation.

Follow the spec-driven documentation workflow:

1. Identify the implemented behavior from the code, UI pages, services, models, storage, tests, and configuration.
2. Create or update the matching feature spec in docs/features/<feature-name>.md.
3. In the feature spec, make sure these sections are accurate:
   - Status
   - Current Implementation
   - Requirements
   - User Flows
   - UI Expectations
   - Data Model Impact
   - Authorization Rules
   - Acceptance Criteria
   - Tests
   - Outstanding Tasks
   - Change Log
4. Update docs/user-guide/ if the feature has user-visible behavior.
5. Update docs/source-of-truth/ only if the implementation changes or clarifies global product requirements, architecture, roles/permissions, data model, storage, navigation, or UI rules.
6. Move any stale notes from planning docs into the right feature spec, user guide, or source-of-truth doc.
7. Do not change application behavior unless you find a documentation-only typo in code comments or labels that is necessary for accuracy.
8. Do not start the WebApp. I will run it via Visual Studio.
9. Run lightweight validation if useful, such as markdown/link checks if available.
10. Summarize what documentation was updated and what outstanding implementation/documentation gaps remain.