Phone_Book
=====================

***Phone_Book*** is a project that allows people working in one or different organizations 
to quickly find the necessary information about other employees or post offices such as `name`, `job title`, `mobile phone`, `post office index` etc.
***
The application has built-in **global search** or search functions by **several fields**. 
Although the application uses a relational database, many relationships in the organization are not 
clearly structured, but the application displays this information in the most understandable way.
***
The project also uses the developments from my other project - ***SQL_Strategy***, which allows, if necessary, 
to use different drivers when connecting to the database. The use of this tool was rather a forced necessity 
due to frequent migration of servers from different OS, but I hope this approach will find its application in other situations.

Main files description
=====================
File/Directory Name     | Content
------------------------|----------------------
HomeController.cs       | Controller with processing of business logic and HTTP requests
DbModels.cs             | Classes to map SQL data by DBcontext
Models/Dto              | Custom classes to store multiple entities as a Model for .cshtml views
_Layout.cshtml          | Basic layout to be the applied to all .cshtml views
Index.cshtml            | Start page containing main information about structural units of company
Management.cshtml       | Razor page for management staff display
RupsConcrete.cshtml     | Detailed information about a department
UpsInfoView.cshtml      | Detailed information about a sub department
PersonViewer.cshtml     | Employee of a single structural unit information
wwwroot/css             | .css styles to be linked to Razor pages
Repo.cs                 | Repository of two different strategies to gather database information
appsettings.json        | Server hosting information including allowed hosts, app URL etc.
