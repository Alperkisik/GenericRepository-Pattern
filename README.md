# GenericRepository-Pattern for ADO.NET and DAPPER

- Safe for SQL Injection Attacks. Functions only Accepts parameters on raw sql querries.
- Totally Generic queries. Queries is generated automatically when there is no query given in dataAcccess functions.
- Raw querries will be generated according to generic table name and parameters when there is no query given in dataAcccess functions if CommandType option is given Text otherwise Stored Procedure queries will be generated.
- Compatible with SQl Stored Procedures
- Generic Stored Procedures queries are generated when there is no query given in dataAccess functions.
- Generic Stored Procedures queries generetad with pattern following that for select query "[dbo].[Sel_{TableName}]", for Count query "[dbo].[Sel_{TableName}_Count]", for Any query "[dbo].[Sel_{TableName}_Any]", for insert or update query "[dbo].[Up_{TableName}]".
- InsertOrUpdate option returns Id back not affected query count or true,false.
- Includes Generic Repository Pattern with ExampleModel Entity
<br>

> [!NOTE]
> - Works better if models has [Table("{Table Name}")] and their id fields has [key] attribute
> - Not compatible with [NotMapped] attribute on models properties
> - Works better with cheking data validations before using data access functions.
