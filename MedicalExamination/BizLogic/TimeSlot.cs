﻿namespace MedicalExamination
{
    public class TimeSlot
    {
       
        //Timeslots are from 7 to 19, devided by 1 hour
        //Timeslots are only occupied when there is a doctor and patient id on it
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; } 
    }
}