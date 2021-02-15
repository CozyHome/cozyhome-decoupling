using System;
using System.Text;
using System.Collections.Generic;
namespace com.cozyhome.Console
{
    public static class ConsoleHeader
    {
        enum ParseState { Quote = 0, Standard = 1 }

        public delegate void Command(string[] modifiers, out string output);

        // this is probably the most scuffed algorithm I have ever written.
        public static string[] Parse(string rawinput)
        {
            // find action and subsequent modifiers
            const int MAXKEYS = 10;
            // I hate this but a potential fix is to 
            // regularly call GC.Collect() during our update timeline
            // to prevent spikes...

            // alongside that, maybe make a command that allows for GC.Collect to be ran
            // if it hasn't been manullay been ran for some time ? 

            //rawinput = rawinput.TrimStart();
            //rawinput = rawinput.TrimEnd();
            //String[] tmpbuffer = rawinput.Split(delims, MAXKEYS);

            // so here's a pretty inefficient implementation for identifying chars

            // stack of quote identifiers to know if to ignore splitting
            Queue<int> quotequeue = new Queue<int>(); // quote stack
            Queue<int> charqueue = new Queue<int>();
            char[] txt = rawinput.ToCharArray();
            int wc = 0; // word count

            String[] tmpbuffer = new String[MAXKEYS];
            tmpbuffer[0] = "";

            // first pass: determine quote attributes
            for (int i = 0; i < txt.Length; i++)
                if (txt[i] == '"')
                    quotequeue.Enqueue(i);
                else
                {
                    if (txt[i] == ' ') // ignore whitespace
                        continue;

                    // our cstack will work like this
                    // odd values will represent starting indices
                    // even values will represent ending indices

                    // how to determine end of signature?
                    // well...
                    // odd values will start AFTER a whitespace.
                    // even values will start BEFORE a whitespace.

                    // as well as this, if we're currently determing an even
                    // index, we automatically know to determine an odd index for next iteration

                    // starting index will begin first:

                    bool isInQuote = quotequeue.Count % 2 == 1;

                    bool isStartingIndex = (charqueue.Count & 0x0001) == 0;

                    if (isInQuote)
                        continue;
                    else
                    {
                        if (isStartingIndex)
                        {
                            if (i == 0 || // if is beginning OR
                               txt[i - 1] == ' ' || // prev is whitespace OR
                               txt[i - 1] == '"') // prev is quote
                                charqueue.Enqueue(i);
                        }
                        else // if not starting index, theen we must be an ending index 
                        {
                            if (i + 1 >= txt.Length - 1 ||
                                txt[i + 1] == ' ' ||
                                txt[i + 1] == '"')
                                charqueue.Enqueue(i + 1);
                        }
                    }
                }

            for (; wc < MAXKEYS; wc++)
            {
                int cindex = 1000;
                int qindex = 1000;

                if (charqueue.Count > 0)
                    cindex = charqueue.Peek();

                if (quotequeue.Count > 0)
                    qindex = quotequeue.Peek();

                int delta = qindex - cindex;

                if (delta > 0) // if quotequeue is ahead in index count, queue goes first.
                {
                    if (charqueue.Count > 1)
                    {
                        int c0 = charqueue.Dequeue();
                        int c1 = charqueue.Dequeue();
                        tmpbuffer[wc] = rawinput.Substring(c0, c1 - c0);
                    }
                }
                else
                {
                    if (quotequeue.Count > 1)
                    {
                        int c0 = quotequeue.Dequeue();
                        int c1 = quotequeue.Dequeue();
                        tmpbuffer[wc] = rawinput.Substring(c0, c1 - c0 + 1);
                    }
                }
            }

            return tmpbuffer;
        }
    }
}
