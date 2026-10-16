# RaceDay API — Implemented Endpoints (Part 2)

This document lists every endpoint the API exposes. Request data is sent as **form fields** (not JSON), so each field appears as its own text box in Swagger. Dates use `yyyy-MM-dd` and times use `hh:mm:ss`.

**Role column**

- **Public**: no login needed
- **Any**: any logged-in user
- **Organiser** / **Participant**: that role only
- **(owner)**: the Organiser must own the event, or the Participant must own the enrolment, otherwise 403

Every endpoint except register and login returns **401 Unauthorized** when there is no session, and **403 Forbidden** when the role is wrong or the owner check fails. Validation failures return **400 Bad Request**.

## Users (5)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| POST | /api/Users/register | Creates a new Participant account | Public | fullName, email, password | 201 Created (user details, no password); 400 invalid data; 409 email already used |
| POST | /api/Users/login | Logs in and starts a session | Public | email, password | 200 OK (name and role); 400 invalid data; 401 wrong email or password |
| POST | /api/Users/logout | Ends the current session | Any | None | 200 OK; 401 not logged in |
| GET | /api/Users/profile | Returns the logged-in user's profile | Any | None | 200 OK; 401; 404 |
| PUT | /api/Users/profile | Updates the logged-in user's name and email | Any | fullName, email | 200 OK; 400; 401; 404; 409 email used by another account |

## Events (5)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| GET | /api/Events | Lists all events, soonest first | Any | None | 200 OK; 401 |
| GET | /api/Events/{id} | Returns one event | Any | None | 200 OK; 401; 404 |
| POST | /api/Events | Creates an event owned by the logged-in Organiser | Organiser | name, description, eventDate, location, eventType (Running, Cycling or Walking) | 201 Created; 400 (invalid data, past date, invalid type); 401; 403 |
| PUT | /api/Events/{id} | Updates an event | Organiser (owner) | name, description, eventDate, location, eventType | 200 OK; 400; 401; 403; 404 |
| DELETE | /api/Events/{id} | Deletes an event that has no categories | Organiser (owner) | None | 204 No Content; 401; 403; 404; 409 event still has categories |

## Categories (5)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| GET | /api/Categories?eventId= | Lists categories, optionally for one event, with spots remaining | Any | None (optional query: eventId) | 200 OK; 401 |
| GET | /api/Categories/{id} | Returns one category | Any | None | 200 OK; 401; 404 |
| POST | /api/Categories | Creates a category for an event | Organiser (owner) | eventID, name, distanceKm, maxParticipants, entryFee | 201 Created; 400; 401; 403; 404 event not found; 409 duplicate name in event |
| PUT | /api/Categories/{id} | Updates a category | Organiser (owner) | name, distanceKm, maxParticipants, entryFee | 200 OK; 400 (limit below current enrolments); 401; 403; 404; 409 |
| DELETE | /api/Categories/{id} | Deletes a category with no enrolments or routes | Organiser (owner) | None | 204 No Content; 401; 403; 404; 409 |

## Routes (5)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| GET | /api/Routes?categoryId= | Lists routes, optionally for one category | Any | None (optional query: categoryId) | 200 OK; 401 |
| GET | /api/Routes/{id} | Returns one route | Any | None | 200 OK; 401; 404 |
| POST | /api/Routes | Creates a route for a category | Organiser (owner) | categoryID, routeName, distanceKm, elevationGain, mapUrl | 201 Created; 400; 401; 403; 404 category not found |
| PUT | /api/Routes/{id} | Updates a route | Organiser (owner) | routeName, distanceKm, elevationGain, mapUrl | 200 OK; 400; 401; 403; 404 |
| DELETE | /api/Routes/{id} | Deletes a route | Organiser (owner) | None | 204 No Content; 401; 403; 404 |

## Enrolments (6)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| POST | /api/Enrolments | Enters the logged-in Participant into a category and assigns a bib number | Participant | categoryID | 201 Created (status Active, bib number); 400 (event already took place); 401; 403; 404 category not found; 409 (already enrolled, or category full) |
| GET | /api/Enrolments/my | Lists the logged-in Participant's own enrolments | Participant | None | 200 OK; 401; 403 |
| GET | /api/Enrolments?eventId= | Lists all enrolments for the Organiser's own events | Organiser | None (optional query: eventId) | 200 OK; 401; 403 |
| GET | /api/Enrolments/{id} | Returns one enrolment | Participant (owner) or Organiser (owner of the event) | None | 200 OK; 401; 403; 404 |
| PUT | /api/Enrolments/{id}/cancel | Cancels the Participant's own enrolment | Participant (owner) | None | 200 OK (status Cancelled); 400 already cancelled; 401; 403; 404; 409 result already captured |
| DELETE | /api/Enrolments/{id} | Removes an enrolment from the Organiser's own event | Organiser (owner) | None | 204 No Content; 401; 403; 404; 409 result exists |

## Results (5)

| Method | Route | Description | Role | Form fields | Responses |
|---|---|---|---|---|---|
| GET | /api/Results?eventId= | Lists results for the Organiser's own events | Organiser | None (optional query: eventId) | 200 OK; 401; 403 |
| GET | /api/Results/my | Lists the logged-in Participant's own results | Participant | None | 200 OK; 401; 403 |
| POST | /api/Results | Captures a finish time and position (marks the enrolment Completed) | Organiser (owner) | enrolmentID, finishTime, position | 201 Created; 400 (invalid time, cancelled enrolment); 401; 403; 404; 409 (result exists, or position taken in the category) |
| PUT | /api/Results/{id} | Corrects a captured result | Organiser (owner) | finishTime, position | 200 OK; 400; 401; 403; 404; 409 |
| DELETE | /api/Results/{id} | Deletes a result (enrolment returns to Active) | Organiser (owner) | None | 204 No Content; 401; 403; 404 |

## Differences from the Part 1 endpoint plan

| Change | Reason |
|---|---|
| Authentication endpoints moved from an Auth area to `/api/Users/...` | Auth was merged into Users on the lecturer's instruction |
| Request bodies are form fields, not JSON | The lecturer asked for a text box per field in Swagger |
| Added `/api/Users/logout` | Sessions need a way to end |
| Added `/my` endpoints for enrolments and results | Participants view only their own data |
| Added `PUT /api/Enrolments/{id}/cancel` | Participants can withdraw without deleting the record |
| Added `?eventId=` and `?categoryId=` filters | Needed to list items for one event or category |
| Distance is stored on categories, not events | One event offers several distances |
| Public registration creates Participants only | Organisers are created by the seeder, so nobody can self-register as an Organiser |

*Compare this table with the Part 1 plan in `docs` and edit it so it lists exactly what differs.*
