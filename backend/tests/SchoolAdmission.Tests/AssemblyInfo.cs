using Xunit;

// Disable parallelization across test classes so high-concurrency suites
// (1000 concurrent requests) have full access to database connection pool
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Ensure concurrency benchmark tests run last so their summary blocks are printed at the very end
[assembly: TestCaseOrderer("SchoolAdmission.Tests.PriorityTestOrderer", "SchoolAdmission.Tests")]
[assembly: TestCollectionOrderer("SchoolAdmission.Tests.PriorityTestOrderer", "SchoolAdmission.Tests")]
