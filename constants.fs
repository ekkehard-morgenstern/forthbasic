\ Forth BASIC v0.1 - A BASIC interpreter written in FORTH.
\ Copyright (C) 2019 Ekkehard Morgenstern
\ 
\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <https://www.gnu.org/licenses/>.
\ 
\ Programs written in Forth BASIC do not fall under this license except 
\ where otherwise noted.
\ 
\ This version has been written for use with GNU Forth (aka GForth). 
\ Contact info: ekkehard@ekkehardmorgenstern.de

\ === CONSTANTS =========================================================================

13 constant bc-cr
10 constant bc-lf
27 constant bc-escape
32 constant bc-space
45 constant bc-minus
48 constant bc-digit-lo
57 constant bc-digit-hi
0 constant bc-false
-1 constant bc-true
256 constant bc-max-line-size
128 constant bc-max-lines
7 constant bc-fg-vmsk
7 constant bc-bg-vmsk
3 constant bc-mode-vmsk
8 constant bc-fg-vshf
11 constant bc-bg-vshf
14 constant bc-mode-vshf
0xe000 constant bc-mode-mask
1 constant bc-def-mode
4 constant bc-def-bgcol
3 constant bc-def-fgcol
1 constant bc-mark-mode
5 constant bc-mark-bgcol
3 constant bc-mark-fgcol

