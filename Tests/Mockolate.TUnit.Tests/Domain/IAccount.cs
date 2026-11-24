namespace Mockolate.TUnit.Tests.Domain;

public interface IAccount
{
	void Withdraw(decimal amount);
	void Deposit(decimal amount);
}
