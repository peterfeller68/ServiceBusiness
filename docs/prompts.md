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


Review section #12 and update the Current Implementation paragraph

Review section #14 and add the Current Implementation paragraph

Focus on section 14. Looking at section 14, what changes do you need to make to the current product?

Go ahead and implement the identified changes and update the Current Implementation paragraphs.

Go ahead and implement the updates section 14 and update the Current Implementation paragraphs.

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