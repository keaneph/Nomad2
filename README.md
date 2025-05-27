# Nomad
A Bike Rental Management System (version 1.0.0) developed using Windows Presentation Foundation (WPF), designed to streamline and empower the management of bike rentals, customers, payments, and returns.

## Dashboard Demo
*Coming soon: Demo screenshots and videos!*

## Notes and Remarks
This project is under active development. The current version is a prototype and is intended for demonstration purposes only.


If you encounter an issue such as "Run a NuGet package restore to generate this file", please follow these steps:

**(In Visual Studio)** From Tools > NuGet Package Manager > Package Manager Console, simply run:
```
dotnet restore
```
This error occurs because the dotnet CLI does not create all required files initially. Running `dotnet restore` will add the necessary files.

## Features
- Dashboard Overview
- Customer Management
- Bike Management
- Rental and Return Tracking
- Payment Management
- Search and Filtering

## Installation
1. Clone the repository:
   ```
   git clone https://github.com/keaneph/Nomad
   ```
2. Open the project (`Nomad2.sln`) in Visual Studio
3. Build the project
4. Run the project

## Roadmap
- Migration to a dynamic web application (ASP.NET Core)
- Enhanced analytics and reporting
- User roles and permissions
- Improved data validation
- Data export, import, backup, and restore
- Database integration (SQL Server, MySQL, PostgreSQL)
- Mobile app companion

## Contributing
Contributions are always welcome!

Please read `CONTRIBUTING.md` for details on the code of conduct and the process for submitting pull requests.

## Built With
- C# (.NET Framework)
- Windows Presentation Foundation (WPF)
- XAML

## Author
[@keaneph](https://github.com/keaneph) - Initial work

## License
This project is licensed under the MIT License - see the `LICENSE.txt` for details

## Acknowledgements
- @mustafamclngn - for his blatant discouragement 
- @xbryan25 - for his wavering support