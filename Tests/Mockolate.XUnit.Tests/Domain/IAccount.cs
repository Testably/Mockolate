namespace Mockolate.XUnit.Tests.Domain;

public interface IAccount
{
	void Withdraw(decimal amount);
	void Deposit(decimal amount);
}
