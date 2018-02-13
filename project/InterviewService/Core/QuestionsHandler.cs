using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CognitiveServices.SpeechRecognition;

namespace SpeechToTextWPF
{
    /// <summary>
    /// Set the questions in order
    /// </summary>
    class QuestionsHandler
    {
        /// <summary>
        /// Pointer of current question
        /// </summary>
        private int index = 0;

        /// <summary>
        /// Text to speach instance
        /// </summary>
        TTS speach;

        /// <summary>
        /// Reservoir of questions
        /// </summary>
        List<string> questions = new List<string>(new string[] {
            "What do you Think about Programing?", 
            "How you even thought of change from you faborite proraming language to other? ... and Why?", 
            "What is a SOLID for you?",
            "What the patterns made inside your work?",
            "What for is your real passion? What you really want to do?",
            "Thank you for your time. GoodBye."
        });

        public QuestionsHandler()
        {
            speach = new TTS();
        }

        /// <summary>
        /// Set the pointer to 0
        /// </summary>
        public void clean() {
            index = 0;
        }

        /// <summary>
        /// Get the next question from the reservoir
        /// 
        /// TODO: make this function asyncronous
        /// 
        /// <param name="microphoneReference">reference to the microphone client handler</param>
        /// <param name="firstQuestion">set whether the end Recognition is going to be called </param>
        /// </summary>
        public string Next(ref MicrophoneRecognitionClient microphoneReference, bool firstQuestion = false)
        {
            string currentQuestion = "";
            if (!firstQuestion)
            {
                microphoneReference.EndMicAndRecognition();
            }
            else
            {
                speach.Start();
            }

            if (index <= questions.Count - 1)
            {
                currentQuestion = questions[index];
                speach.Speak(currentQuestion);
                index++;
                microphoneReference.StartMicAndRecognition();
            }

            return currentQuestion;
        }

        public bool isOver()
        {
            return (index > questions.Count - 1);
        }
    }
}
