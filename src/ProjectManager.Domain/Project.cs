namespace ProjectManager.Domain;

/// <summary>
/// A company project. Cannot be constructed in an invalid state — all mutation
/// goes through the factory/Update which enforce invariants. This is the last-line
/// backstop; primary input validation happens in the Application layer.
/// </summary>
public sealed class Project
{
    public string Id { get; }
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }
    public string Customer { get; private set; }

    private Project(string id, string name, string abbreviation, string customer)
    {
        Id = id;
        Name = name;
        Abbreviation = abbreviation;
        Customer = customer;
    }

    public static Project Create(string id, string name, string abbreviation, string customer)
    {
        Require(id, nameof(id));
        Require(name, nameof(name));
        Require(abbreviation, nameof(abbreviation));
        Require(customer, nameof(customer));

        return new Project(id.Trim(), name.Trim(), abbreviation.Trim(), customer.Trim());
    }

    public void Update(string name, string abbreviation, string customer)
    {
        Require(name, nameof(name));
        Require(abbreviation, nameof(abbreviation));
        Require(customer, nameof(customer));

        Name = name.Trim();
        Abbreviation = abbreviation.Trim();
        Customer = customer.Trim();
    }

    private static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{field}' must not be empty.", field);
    }
}
