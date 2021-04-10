Aptus Evolution - An Evolution Simulation
Copyright(C) 2020 - Brendan Price

About Aptus Evolution

The Aptus evolution program shows a process of evolution via natural selection.

It is governed by the following rules, and these rules only: 
	# Food 'red' falls randomly, and each food item 'eaten' gives 6 energy points to the organism
	# Initially, Single-cell'd organisms are created
	##They have, by default, a movement direction.
	##For an organism to replicate, it must meet a certain energy threshold.
	## The energy threshold required for an organism to replicate is calculated by its size (measured in cells) x 10 energy points
	##There is a 1-3% chance of a mistake occurring during replication
	##Mutations affect the genetic code of an organism, governing structure, movement, and speed
	# When a collision occurs between 2 organisms, force is used to determine the winner
	## Force is calculated as 'amount of cells' x 'speed', the organism with the greatest force kills the other organism, in which that killed organism now appears as 'red food'

That is the rules, everything is governed by them.

Please See video of Aptus Evolution:
https://youtu.be/2Yu4BVcjoro

For Developers:
 - AptusEvolution proj contains the source code
 - AptusEvolutionInstaller proj contains the installation code
 
 Other
  - AptusEvolutionInstallationFiles contains the Windows Installer file to install the program to your PC.
