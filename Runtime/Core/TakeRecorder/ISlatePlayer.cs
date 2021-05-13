namespace Unity.LiveCapture
{
    interface ISlatePlayer
    {
        int GetSlateCount();
        ISlate GetSlate(int index);
        ISlate GetActiveSlate();
        double GetTime();
        void SetTime(double value);
        void SetTime(ISlate slate, double value);
    }
}
