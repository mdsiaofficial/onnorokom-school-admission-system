using Xunit;

// Disable parallelization across test classes so high-concurrency suites
// (200 concurrent requests) have full access to database connection pool
[assembly: CollectionBehavior(DisableTestParallelization = true)]
