# Introduction
The application allows users to communicate with a server via a browser to create an account and join small groups, which may have requirements for potential members. The application targets video gamers.

Code and UI is written in polish, english branch is in progress.

It was written as an engineering thesis.

# Technologies
- **Backend** - ASP.NET + Entity Framework
- **Frontend** - React.js
- **Database** - MS SQL Server

# System modules
The system is a modular monolith in which the browser-based application communicates with the backend via an API, and the backend communicates with the database using queries, mostly managed by Entity Framework. System modules are as follows:
- **Game Library** (*Biblioteka Gier*) – handles games owned by the user. It populates the profile page table with them and handles updates and deletions.
- **Teams** (*Drużyny*) – handles retrieving team information, as well as creating, searching for, updating, and deleting teams. It also manages slots for team members and provides data for team-related forms.
- **External Integrations** (*Integracje zewnętrzne*) – handles communication between "internal" and "external" tables. The repository for this module is the only class that "knows" the external service is a mock/external entity.
- **Platforms** (*Platformy*) – handles information about game platforms (PC, PlayStation etc.) and their associations with users.
- **Notifications** (*Powiadomienia*) – handles notifications, including storage, sending, and processing.
- **Profiles** (*Profile*) – handles user profiles — everything visible on the user profile page. Internally, repositories, services, and controllers are organized by regions, countries, statuses, languages, language proficiency levels, and profiles themselves.
- **Statistics** (*Statystyki*) – handles user in-game statistics and the team roles associated with some of them. It retrieves, compares, and updates statistics. It also includes an additional "ranks" table used to determine whether a specific statistic qualifies as a rank.
- **Users** (*Użytkownicy*) – handles user accounts, including retrieving information, creating accounts, updating data, and deleting accounts. It also manages login and registration using Identity technology.
- **Messages** (*Wiadomości*) – handles the sending and receiving of private and team messages. 
- **Supported Games** (*Wspierane gry*) – handles information about games supported by the service and returns details about them.
- **Acquaintances** (*Znajomości*) – manages "acquaintances" between accounts—adding friends, removing friends, and returning the friends list.

The system is designed to communicate with a separate, independent server that collects in-game player statistics. For this project, that server is replaced by tables in a separate schema that are not managed by Entity Framework. A database job runs daily at 4:00 PM to synchronize user statistics with the data from these "external" tables. Based on this statistical data, users can set requirements for joining the team they are creating.

# Example screens
## "Your Teams"
<img width="943" height="510" alt="obraz" src="https://github.com/user-attachments/assets/b9e26b61-7c68-42cf-afcc-2c4976182657" />  

## "Your Profile"
<img width="944" height="1264" alt="obraz" src="https://github.com/user-attachments/assets/5372bd12-880f-40f2-9ec8-b60ae26ddccf" />  

## "Create a Team"
<img width="1919" height="2170" alt="obraz" src="https://github.com/user-attachments/assets/f8374321-2244-4add-9996-2ddf7689cd4f" />

## "Team Details"
<img width="917" height="1381" alt="obraz" src="https://github.com/user-attachments/assets/f972352d-7d3c-4790-a238-0566133f6a79" />

**Version of the list of the team members, when the user is the capitan of that team**
<img width="945" height="610" alt="obraz" src="https://github.com/user-attachments/assets/228e0b3e-8fff-4cbe-94db-bcbea69fb1a6" />
