using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SchoolAdmission.Tests;

public class PriorityTestOrderer : ITestCaseOrderer, ITestCollectionOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc => GetPriority(tc.TestMethod.TestClass.Class.Name));
    }

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        return testCollections.OrderBy(tc => GetPriority(tc.DisplayName));
    }

    private static int GetPriority(string? name)
    {
        if (string.IsNullOrEmpty(name)) return 10;
        if (name.Contains("ConcurrencyOversellingTests")) return 100;
        if (name.Contains("ConcurrencySingleActiveApplicationTests")) return 99;
        if (name.Contains("ConcurrencyIdempotentConfirmTests")) return 98;
        if (name.Contains("HoldExpiryTests")) return 50;
        if (name.Contains("ApiContractTests")) return 30;
        if (name.Contains("ValidationTests")) return 10;
        return 20;
    }
}
