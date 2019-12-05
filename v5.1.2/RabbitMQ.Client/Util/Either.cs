namespace RabbitMQ.Util
{
    ///<summary>Used internally by class Either.</summary>
    public enum EitherAlternative
    {
        Left,
        Right
    }

    ///<summary>Models the disjoint union of two alternatives, a
    ///"left" alternative and a "right" alternative.</summary>
    ///<remarks>Borrowed from ML, Haskell etc.</remarks>
    public class Either
    {
        ///<summary>Private constructor. Use the static methods Left, Right instead.</summary>
        private Either(EitherAlternative alternative, object value)
        {
            Alternative = alternative;
            Value = value;
        }

        ///<summary>Retrieve the alternative represented by this instance.</summary>
        public EitherAlternative Alternative { get; }

        ///<summary>Retrieve the value carried by this instance.</summary>
        public object Value { get; }

        ///<summary>Constructs an Either instance representing a Left alternative.</summary>
        public static Either Left(object value)
        {
            return new Either(EitherAlternative.Left, value);
        }

        ///<summary>Constructs an Either instance representing a Right alternative.</summary>
        public static Either Right(object value)
        {
            return new Either(EitherAlternative.Right, value);
        }
    }
}
