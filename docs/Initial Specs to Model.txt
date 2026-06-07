I want to develop a SaaS based service to help automate the workflow used by pool cleaning businesses and landscapers. The main customer of this service is a small pool cleaning or landscaping business. Those businesses themselves have clients, which are mostly home owners. The service will be hosted on Azure and the UI should be written in Blazor. The data shall all be persisted within Azure storage. The authentication will be handled via Standard Google Authentication.

Personas:
 System Administrator
 The system administrator manages the SaaS service. 
  Manage Company Types (Pool Cleaning Service, Landscaping Service)
  A company is essentially a client of the Saas Service
  Manage Data needed - 

 Company Admin
 The company admin is generally the owner of the pool cleaning business or landscaping business and maintains all data and settings associated with the Business Client
 - Needs the ability to maintain the configuration for a company
 - Configurations for companies entail the following:
   - Manage company users
   - Manage Services that they offer
   - Manage Materials that they offer
   - Manage Schedules
   - Manage Company Clients to be serviced on a specific date
   - Assign Company Clients to be serviced to a Company User
 - Needs the ability to create a new Company client (self-service)
 - Needs the ability to approve company user requests to join the company. These user requests will come from employees of the pool cleaning or landscaping business. They will be added a standard company users.
- Needs the ability to approve company client requests. These user requests will come from home owners of the pool cleaning or landscaping business. They will be added as company client user.

 - Ability to setup Client Types, generally the model for reimbursement. It could be Fee-For-Service, or a weekly, bi-weekly or monthly service. The weekly, bi-weekly and monthly service have a standard rate, but can be changed for each business client.
 - Payments will be managed through Stripe
Reports
Provide on demand daily, weekly, monthly or custom date range reports by user, client via UI


Standard Company User
 - Functionality available via mobile and computer based application. Most interaction will occur via mobile app.
 - See all Company Clients assigned for service
 - Figure out an optimal route for all scheduled clients
 - When at a client have the ability to provide information about the service provided
   - pick from a dropdown of services setup by the admin
   - pick from a dropdown of materials setup by the admin
 - When the service completes, an email will be generated to the company client about the service performed. There needs to be the ability to provide free form notes that get persisted along with the service information


Company client user
 - Functionality available via mobile and computer based application. Most interaction will occur via mobile app.
 - Ability to see a list of services provided to them
 - Ability to see history of bills and payments
 - Ability to message the Company with questions



Given these specs, come up with detailed requirements that can be fed into codex to generate the application.
Provide an architecture document
Provide a storage entity list 
Provide UI specifications - list each screen for each Persona
 