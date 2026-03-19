import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-adult-hockey',
  imports: [RouterLink],
  templateUrl: './adult-hockey.html',
  styleUrl: './adult-hockey.css',
})
export class AdultHockey {
  leagues = [
    {
      name: 'A Division',
      schedule: [
        { home: 'Ice Wolves', away: 'Blue Bolts', date: 'Feb 24', time: '8:00 PM', rink: 'Rink 1' },
        { home: 'Freeze FC', away: 'Slap Shots', date: 'Feb 25', time: '9:15 PM', rink: 'Rink 2' },
        { home: 'Blue Bolts', away: 'Freeze FC', date: 'Feb 27', time: '7:30 PM', rink: 'Rink 1' },
      ],
      standings: [
        { name: 'Ice Wolves',  w: 8, l: 2, t: 1, pts: 17 },
        { name: 'Blue Bolts',  w: 7, l: 3, t: 1, pts: 15 },
        { name: 'Freeze FC',   w: 5, l: 5, t: 1, pts: 11 },
        { name: 'Slap Shots',  w: 2, l: 8, t: 1, pts: 5  },
      ],
      recentScores: [
        { home: 'Ice Wolves', homeScore: 4, awayScore: 2, away: 'Slap Shots', date: 'Feb 20' },
        { home: 'Blue Bolts', homeScore: 3, awayScore: 3, away: 'Freeze FC',  date: 'Feb 19' },
        { home: 'Freeze FC',  homeScore: 5, awayScore: 1, away: 'Ice Wolves', date: 'Feb 17' },
      ],
    },
    {
      name: 'B Division',
      schedule: [
        { home: 'Puck Ducks', away: 'Hat Tricks', date: 'Feb 24', time: '6:30 PM', rink: 'Rink 2' },
        { home: 'Zamboni Riders', away: 'Puck Ducks', date: 'Feb 26', time: '8:45 PM', rink: 'Rink 1' },
        { home: 'Hat Tricks', away: 'Zamboni Riders', date: 'Feb 28', time: '7:00 PM', rink: 'Rink 2' },
      ],
      standings: [
        { name: 'Hat Tricks',     w: 9, l: 1, t: 0, pts: 18 },
        { name: 'Puck Ducks',     w: 6, l: 4, t: 1, pts: 13 },
        { name: 'Zamboni Riders', w: 4, l: 6, t: 1, pts: 9  },
        { name: 'Odd Icing',      w: 1, l: 9, t: 1, pts: 3  },
      ],
      recentScores: [
        { home: 'Hat Tricks',     homeScore: 6, awayScore: 2, away: 'Odd Icing',      date: 'Feb 21' },
        { home: 'Puck Ducks',     homeScore: 2, awayScore: 4, away: 'Zamboni Riders', date: 'Feb 19' },
        { home: 'Zamboni Riders', homeScore: 3, awayScore: 3, away: 'Hat Tricks',     date: 'Feb 18' },
      ],
    },
    {
      name: 'C Division',
      schedule: [
        { home: 'Rec Stars',  away: 'Ice Breakers', date: 'Feb 23', time: '6:00 PM', rink: 'Rink 1' },
        { home: 'Pond Rats',  away: 'Rec Stars',    date: 'Feb 25', time: '7:15 PM', rink: 'Rink 2' },
        { home: 'Ice Breakers', away: 'Pond Rats',  date: 'Feb 27', time: '6:00 PM', rink: 'Rink 2' },
      ],
      standings: [
        { name: 'Ice Breakers', w: 7, l: 2, t: 2, pts: 16 },
        { name: 'Pond Rats',    w: 6, l: 3, t: 2, pts: 14 },
        { name: 'Rec Stars',    w: 4, l: 5, t: 2, pts: 10 },
        { name: 'Snow Plow',    w: 1, l: 9, t: 1, pts: 3  },
      ],
      recentScores: [
        { home: 'Ice Breakers', homeScore: 3, awayScore: 1, away: 'Snow Plow', date: 'Feb 20' },
        { home: 'Pond Rats',    homeScore: 4, awayScore: 4, away: 'Rec Stars', date: 'Feb 18' },
        { home: 'Rec Stars',    homeScore: 2, awayScore: 5, away: 'Pond Rats', date: 'Feb 16' },
      ],
    },
  ];
}

