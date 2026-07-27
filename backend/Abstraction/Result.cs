namespace Marketify.Abstraction
{
    public class Result
    {
        public Result(bool isSuccess , Error error)
        {
            if ((isSuccess == true && error != Error.None) || (!isSuccess && error == Error.None))
                throw new InvalidOperationException();

            IsSuccess = isSuccess ;
            Error = error ;
        }
        public bool IsSuccess { get;  }
        public bool IsFailure =>!IsSuccess;
        public Error Error { get; } = default!;
        public static Result Success()=>new Result(true, Error.None);
        public static Result Failure(Error error)=>new Result(false, error);

        public static Result<TValue> Success<TValue>(TValue value)
             => new(value, true, Error.None);
         public static Result<TValue> Failure<TValue>(Error error)
            => new( default,false, error);
    }
    public class Result<TValue> :Result
    {
        private TValue ?_value;
        public Result(TValue? value , bool iSuccess ,Error error ):base(iSuccess, error)
        {
            _value = value;

        }
        public TValue? Value => IsSuccess ?_value : 
            throw new InvalidOperationException("Failure Result");
    }
}
