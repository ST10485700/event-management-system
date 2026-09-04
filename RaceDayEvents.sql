-- Create the RaceDay database
create database RaceDayDB;

-- Use the RaceDay database
use RaceDayDB;


-- Create the Users table
create table Users(
    UserID int primary key,
    FullName varchar(100) not null,
    Email varchar(150) not null unique,
    PasswordHash varchar(255) not null,
    Role varchar(20) not null default 'Participant',
    CreatedAt datetime not null default getdate(),

    check (Role in ('Organiser', 'Participant'))
);


-- Create the Events table
create table Events(
    EventID int primary key,
    OrganiserID int not null,
    Name varchar(150) not null,
    Description varchar(500),
    EventDate date not null,
    Location varchar(150) not null,
    EventType varchar(30) not null,

    foreign key (OrganiserID) references Users(UserID),

    check (EventType in ('Running', 'Cycling', 'Walking'))
);


-- Create the Categories table
create table Categories(
    CategoryID int primary key,
    EventID int not null,
    Name varchar(100) not null,
    DistanceKm decimal(6,2) not null,
    MaxParticipants int not null,
    EntryFee decimal(10,2) not null default 0.00,

    foreign key (EventID) references Events(EventID),

    check (DistanceKm > 0),
    check (MaxParticipants > 0),
    check (EntryFee >= 0),

    unique (EventID, Name)
);


-- Create the EventEnrolments table
create table EventEnrolments(
    EnrolmentID int primary key,
    ParticipantID int not null,
    CategoryID int not null,
    EnrolmentDate datetime not null default getdate(),
    Status varchar(20) not null default 'Active',
    BibNumber varchar(20) unique,

    foreign key (ParticipantID) references Users(UserID),
    foreign key (CategoryID) references Categories(CategoryID),

    check (Status in ('Active', 'Cancelled', 'Completed')),

    unique (ParticipantID, CategoryID)
);


-- Create the Results table
create table Results(
    ResultID int primary key,
    EnrolmentID int not null,
    FinishTime time not null,
    Position int not null,
    CapturedByUserID int not null,
    CapturedAt datetime not null default getdate(),

    foreign key (EnrolmentID) references EventEnrolments(EnrolmentID),
    foreign key (CapturedByUserID) references Users(UserID),

    check (Position > 0),

    unique (EnrolmentID)
);


-- Create the Routes table
create table Routes(
    RouteID int primary key,
    CategoryID int not null,
    RouteName varchar(150) not null,
    DistanceKm decimal(6,2) not null,
    ElevationGain int not null default 0,
    MapUrl varchar(300),

    foreign key (CategoryID) references Categories(CategoryID),

    check (DistanceKm > 0),
    check (ElevationGain >= 0)
);


-- Insert data into Users
insert into Users
(UserID, FullName, Email, PasswordHash, Role)
values
(1, 'Thabo Mokoena', 'thabo@raceday.co.za',
 '8f14e45fceea167a5a36dedd4bea2543', 'Organiser'),

(2, 'Lerato Dlamini', 'lerato@raceday.co.za',
 'c9f0f895fb98ab9159f51fd0297e236d', 'Organiser'),

(3, 'Matodzi Nevhutala', 'matodzi@raceday.co.za',
 '45c48cce2e2d7fbdea1afc51c7c6ad26', 'Participant'),

(4, 'Uche Brandan', 'uche@raceday.co.za',
 '6512bd43d9caa6e02c990b0a82652dca', 'Participant');


-- Insert data into Events
insert into Events
(EventID, OrganiserID, Name, Description, EventDate, Location, EventType)
values
(1, 1, 'Johannesburg City Run',
 'A road running event in Johannesburg',
 '2026-10-10', 'Johannesburg', 'Running'),

(2, 1, 'Pretoria Cycle Challenge',
 'A cycling event for different skill levels',
 '2026-11-07', 'Pretoria', 'Cycling'),

(3, 2, 'Rosebank Charity Walk',
 'A community walking event supporting charity',
 '2026-12-05', 'Rosebank', 'Walking');


-- Insert data into Categories
insert into Categories
(CategoryID, EventID, Name, DistanceKm, MaxParticipants, EntryFee)
values
(1, 1, '5km Fun Run', 5.00, 500, 100.00),

(2, 1, '10km Race', 10.00, 750, 180.00),

(3, 1, '21km Half Marathon', 21.10, 1000, 300.00),

(4, 2, '20km Cycle', 20.00, 300, 200.00),

(5, 2, '50km Cycle', 50.00, 500, 350.00),

(6, 3, '5km Community Walk', 5.00, 600, 80.00),

(7, 3, '10km Charity Walk', 10.00, 400, 120.00);


-- Insert data into Routes
insert into Routes
(RouteID, CategoryID, RouteName, DistanceKm, ElevationGain, MapUrl)
values
(1, 1, 'City 5km Route', 5.00, 65,
 'https://example.com/jhb5km'),

(2, 2, 'City 10km Route', 10.00, 120,
 'https://example.com/jhb10km'),

(3, 3, 'Half Marathon Route', 21.10, 240,
 'https://example.com/jhb21km'),

(4, 4, 'Pretoria 20km Cycle Route', 20.00, 180,
 'https://example.com/pta20km'),

(5, 5, 'Pretoria 50km Cycle Route', 50.00, 420,
 'https://example.com/pta50km'),

(6, 6, 'Rosebank 5km Walk Route', 5.00, 55,
 'https://example.com/rb5km'),

(7, 7, 'Rosebank 10km Walk Route', 10.00, 90,
 'https://example.com/rb10km');


-- Insert data into EventEnrolments
insert into EventEnrolments
(EnrolmentID, ParticipantID, CategoryID, BibNumber)
values
(1, 3, 1, 'RD1001'),

(2, 3, 2, 'RD1002'),

(3, 4, 1, 'RD1003'),

(4, 4, 4, 'RD1004'),

(5, 3, 6, 'RD1005');


-- Insert data into Results
insert into Results
(ResultID, EnrolmentID, FinishTime, Position, CapturedByUserID)
values
(1, 1, '00:31:42', 12, 1),

(2, 2, '00:58:16', 8, 1),

(3, 3, '00:34:25', 18, 1);


-- Display all tables
select * from Users;

select * from Events;

select * from Categories;

select * from EventEnrolments;

select * from Results;

select * from Routes;