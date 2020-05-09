namespace RabbitMQ.Util
{
    ///<summary>Used internally by class Either.</summary>
    internal enum EitherAlternative
    {
        Left,
        Right
    }

    ///<summary>Models the disjoint union of two alternatives, a
    ///"left" alternative and a "right" alternative.</summary>
    ///<remarks>Borrowed from ML, Haskell etc.</remarks>
    internal sealed class Either<L, R>
    {
        ///<summary>Private constructor. Use the static methods Left, Right instead.</summary>
        private Either(EitherAlternative alternative, L valueL, R valueR)
        {
            Alternative = alternative;
            LeftValue = valueL;
            RightValue = valueR;
        }

        ///<summary>Retrieve the alternative represented by this instance.</summary>
        public EitherAlternative Alternative { get; }

        ///<summary>Retrieve the value carried by this instance.</summary>
        public L LeftValue { get; private set; }
        public R RightValue { get; private set; }

        ///<summary>Constructs an Either instance representing a Left alternative.</summary>
        public static Either<L, R> Left(L value)
        {
            return new Either<L, R>(EitherAlternative.Left, value, default);
        }

        ///<summary>Constructs an Either instance representing a Right alternative.</summary>
        public static Either<L, R> Right(R value)
        {
            return new Either<L, R>(EitherAlternative.Right, default, value);
        }
    }
}
