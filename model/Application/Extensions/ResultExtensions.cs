using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Application.Extensions
{
    public static class ResultExtensions
    {
        public static Result BindIf(this Result callingResult, bool condition, Func<Result> action)
        {
            return condition && callingResult.IsSuccess ? action() : callingResult;
        }
        
        public static async Task<Result> BindIf(
            this Task<Result> callingResult, bool condition,
            Func<Result> func)
        {
            return condition && (await callingResult.ConfigureAwait(false)).IsSuccess ? 
                func() : 
                await callingResult.ConfigureAwait(false);
        }
    }
}