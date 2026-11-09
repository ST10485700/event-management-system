# RaceDay — Event Management System (Part 3: MVC Web Application)

**Student:** Matodzi Nevhutala (ST10485700)
**Module:** Portfolio of Evidence — Part 3: Automation of Web Application

Part 3 adds the RaceDay web front end: an ASP.NET Core MVC application that gives Organisers and Participants their own role-based interface. The MVC app has no database access and no business rules. Every operation goes through the Part 2 API. Event banners and profile pictures are uploaded to **Azure Blob Storage through the API**, and the MVC app runs in a **Docker** container.

## Links

| Item | Link |
|---|---|
| Part 3 code (branch) | https://github.com/ST10485700/event-management-system/tree/part-3-mvc |
| Part 3 video (unlisted YouTube) | (https://youtu.be/nEZjKztuP6c) |
| Part 2 code (branch) | https://github.com/ST10485700/event-management-system/tree/part-2-api |
| Part 1 code (branch) | https://github.com/ST10485700/event-management-system |

## How it fits together

```
Browser  →  RaceDay_Web (ASP.NET Core MVC, Docker)  →  RaceDay_API (Web API)  →  SQL Server
                                                                    └────────→  Azure Blob Storage (images)
```

- `RaceDay_Web` talks to the API through one class, `RaceDayApiClient`, which sends each user's own API session cookie with every request.
- The MVC app keeps its own session with the user's ID, name and role. A `[RoleAuthorize]` filter sends logged-out users to the login page and sends the wrong role to an "access denied" page.
- Images are sent as files from MVC to the API, the API validates them (JPG, PNG, WEBP or GIF, up to 5 MB), stores them in Azure Blob Storage, and saves the URL in the database. The MVC app never talks to Azure.

## Features

### Organiser
- **Dashboard:** events created, upcoming events, total enrolments and categories, a "Next up" banner, and a table with enrolments per event and days to go.
- **Events:** create, edit and delete events with name, description, date, location, distance and type, plus an **event banner upload** with a live preview.
- **Categories:** add, edit and delete distance or age categories for each event.
- **Enrolments:** pick an event and see every participant with their category, bib number and colour-coded status.
- **Results entry:** capture, update and remove finish times and positions for entered participants.

### Participant
- **Home:** upcoming events with filters (search, type, location, date range, include past events).
- **Event details:** full information, banner and categories, with an **Enter event** button per category that records the enrolment immediately.
- **My enrolments:** every event entered, with a colour-coded status and a cancel option.
- **My results:** event, date, category, finish time and position such as "47th out of 312 finishers".
- **Profile:** update name and email and **upload a profile picture**.

### Interface
Bootstrap layout with a RaceDay sporting theme (navy and orange, a bold condensed heading font), one shared layout and stylesheet for every page, a different menu for each role, colour-coded enrolment statuses (Confirmed green, Completed blue, Cancelled red), friendly error pages, and form validation messages. Raw errors are never shown to users.

## Run it locally

### Prerequisites
- Visual Studio 2022 (or the .NET 8 SDK) and SQL Server LocalDB
- An Azure Storage account if you want to test image uploads (see below)

### Steps
1. Clone the repository and check out the Part 3 branch:
   ```
   git clone https://github.com/ST10485700/event-management-system.git
   cd event-management-system
   git checkout part-3-mvc
   ```
2. Open `RaceDay_API.sln`. Check the connection string in `RaceDay_API/appsettings.json` (it uses the LocalDB instance `(localdb)\api_guide`; change the instance name if yours differs).
3. Right-click the solution, choose **Configure Startup Projects**, and set both `RaceDay_API` and `RaceDay_Web` to **Start**.
4. Press **F5**. The API creates and seeds the database on first start. The website opens on its own port, and Swagger opens for the API.
5. The website reads the API address from `RaceDay_Web/appsettings.json` (`ApiBaseUrl`, default `https://localhost:7184/`).

### Seeded logins
All use the password **`Password123!`**.

| Email | Role |
|---|---|
| thabo@raceday.co.za | Organiser |
| lerato@raceday.co.za | Organiser |
| matodzi@raceday.co.za | Participant |
| uche@raceday.co.za | Participant |

New participants can sign up on the website. Organisers are only created by the seeder.

## Azure Blob Storage

1. Create a Storage account in the Azure portal. Under **Configuration**, enable **Allow Blob anonymous access**.
2. Create a container named `raceday-images` with anonymous access level **Blob**.
3. Copy the account's connection string (**Access keys**).
4. Give it to the API **without committing it**:
   - In Visual Studio: right-click `RaceDay_API`, choose **Manage User Secrets**, and add
     ```json
     {
       "AzureBlobStorage": {
         "ConnectionString": "<your connection string>",
         "ContainerName": "raceday-images"
       }
     }
     ```
   - In a terminal or container, use the environment variable `AzureBlobStorage__ConnectionString`.

Without the connection string the app still works. Image uploads show a friendly "Image storage is not configured" message and events show a placeholder image.

## Run with Docker

The whole system (SQL Server, the API and the MVC website) runs in containers, with a single command. You don't need SQL Server, LocalDB or Visual Studio installed.

### Prerequisites
Docker Desktop (or any Docker with Compose), running.

### Start everything
From the repository folder:
```
docker compose up --build
```
The first build takes a few minutes. When the logs settle, open **http://localhost:8080** for the website (Swagger for the API is at **http://localhost:5099/swagger**). Log in with the seeded accounts above. Stop everything with `Ctrl + C`, or run `docker compose down` (add `-v` to also delete the database).

What starts:

| Container | What it is | Port |
|---|---|---|
| `raceday-db` | SQL Server 2022 | internal |
| `raceday-api` | The RaceDay Web API. It creates and seeds the database on first start (it retries until SQL Server is ready) | 5099 |
| `raceday-web` | The MVC website, which calls the API at `http://api:8080/` | 8080 |

### Image uploads in Docker
To enable event banner and profile picture uploads, create a file named `.env` next to `docker-compose.yml` containing your Azure connection string, then run the command again:
```
AZURE_STORAGE_CONNECTION_STRING=<your connection string>
```
`.env` is ignored by Git, so the key is never committed. Without it, everything works and uploads show "Image storage is not configured".

### Run only the website container
If the API is already running on your computer (for example from Visual Studio, listening on all interfaces on port 5099), you can run just the website:
```
docker build -t raceday-web ./RaceDay_Web
docker run --rm -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5099/ raceday-web
```

The containerised site behaves exactly like the one started from Visual Studio: same pages, same logins, same API.

## Tests

The API unit tests (xUnit) start the real API in memory with an in-memory database and a fake image storage, so they need neither SQL Server nor Azure. Run them in **Test Explorer**, or with:
```
dotnet test RaceDay_API.sln
```

## CI/CD

`.github/workflows/build-and-test.yml` runs on every push and pull request. It validates the repository structure (docs, README, projects, Dockerfiles and compose file), restores and builds the whole solution (API, tests and MVC app), runs all unit tests, then starts the whole system with Docker Compose and smoke-tests it (the API logs in a seeded user and the website's login page responds). `validate-docs.yml` (from Part 1) checks the planning documents.

![Green CI build](https://github.com/user-attachments/assets/2a922abd-8e51-49cd-951a-17c4a0efc829)



## Project structure

```
RaceDay_API.sln
RaceDay_API/            Web API: controllers, EF Core models and migrations, Azure Blob storage service, Dockerfile
RaceDay_API.Tests/      xUnit tests
RaceDay_Web/            MVC app: controllers, view models, Razor views, RaceDayApiClient, Dockerfile
docker-compose.yml      Starts SQL Server, the API and the MVC app
.github/workflows/      CI workflows
docs/                   Planning documents and endpoint documentation
```

## Changes made to the API in Part 3

| Change | Reason |
|---|---|
| `BannerUrl` and `DistanceKm` added to events, `ProfilePictureUrl` added to users (new migration) | Event banner, the event distance field on the Organiser form, and the Participant profile picture |
| Event create and update accept a `banner` file, and the profile update accepts a `profilePicture` file | Images are uploaded through the API to Azure Blob Storage |
| `GET /api/Events` filters (`search`, `eventType`, `location`, `from`, `to`) | Participant home page filtering |
| `GET /api/Events/mine` | Organiser dashboard (own events with category and enrolment counts) |
| Results include `totalFinishers` | "47th out of 312 finishers" on My Results |

## AI Tool Disclosure

AI tools (Claude) were used during Part 3 to guide the build and to help write and debug code: the MVC application, the API additions, the Azure upload service, the Dockerfiles, the Docker Compose file and the workflow, as well as drafts of this documentation. I ran and tested everything myself, made the final decisions, and can explain and defend the code and design choices in the video.

---

*The Part 2 and Part 1 READMEs are kept below.*
