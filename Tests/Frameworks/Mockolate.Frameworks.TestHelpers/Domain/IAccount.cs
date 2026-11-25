namespace Mockolate.Frameworks.TestHelpers.Domain;

public interface IAccount
{
	void Withdraw(decimal amount);
	void Deposit(decimal amount);
}
