namespace Bluepath.Tests.Methods

module DefaultModule =
    /// Computes the sum of the squares of the numbers divisible by 3.
    let sumOfSquaresDivisibleBy3UpTo n = 
        [ 1 .. n ]
        |> List.filter (fun x -> x % 3 = 0)
        |> List.sumBy (fun x -> x * x)
