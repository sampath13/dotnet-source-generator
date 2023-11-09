namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var employeeId = Guid.NewGuid();
            Employee employee1 = new Employee
            {
                Id = employeeId,
                Name = "John Doe",
                SocialSecurityNumber = "123-45-6789"
            };

            Employee employee2 = new Employee
            {
                Id = employeeId,
                Name = "Jane Smith",
                SocialSecurityNumber = "123-45-6789"
            };

            bool areEqual = employee1.Equals(employee2);

            Console.WriteLine("Are the employees equal? " + areEqual);
        }
    }
}