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

\ === EXECUTION =========================================================================

: b-alloc-line ( size -- )
    \ allocates line copy with specified size or nothing if size was zero
    dup dup 0> if                   ( size size )
        chars allocate throw        ( size addr )
    else                            ( size size )
        2drop 0 0                   ( size addr )
    endif
    b-line-copy ! b-line-size ! ;

: b-free-line ( -- )
    \ frees a previously allocated line; does nothing if there was no allocated line
    b-line-copy @ dup 0<> if
        free
        0 b-line-copy !
        0 b-line-size !
    else
        drop
    endif ;

: b-copy-line ( begaddr endaddr -- )
    \ copies line from screen into a dedicated buffer, as characters
    2dup swap - 1+  ( begaddr endaddr count )
    swap drop       ( srcaddr count )
    \ free previous allocated line
    b-free-line     ( srcaddr count )
    \ allocate new line
    dup b-alloc-line    ( srcaddr count )
    b-line-copy @       ( srcaddr count tgtaddr )
    -rot                ( tgtaddr srcaddr count )
    \ copy cells to new buffer as characters
    0 +do               ( tgtaddr srcaddr )
        dup @           ( tgtaddr srcaddr value )
        swap cell+      ( tgtaddr value srcaddr )
        -rot            ( srcaddr tgtaddr value )
        255 and         ( srcaddr tgtaddr value )
        over c!         ( srcaddr tgtaddr )
        char+           ( srcaddr tgtaddr )
        swap            ( tgtaddr srcaddr )
    loop
    2drop ;

: b-execute-line ( -- )
    b-grab-line     ( begaddr endaddr )
    b-copy-line
    b-tokenize-line
;

