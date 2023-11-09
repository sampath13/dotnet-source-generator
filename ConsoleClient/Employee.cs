namespace ConsoleClient;

public partial class Employee
{
    [IncludeInEquals]
    public Guid Id { get; set; }
    public string Name { get; set; }
    [IncludeInEquals]
    public string SocialSecurityNumber { get; set; }
}

public partial class Company
{
    [IncludeInEquals]
    public string Name { get; set; }
}