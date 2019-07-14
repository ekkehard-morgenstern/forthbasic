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

\ === WINDOW RESIZE MANAGEMENT ==========================================================

: b-save-window-info ( -- )
    \ back up window info
    b-window-width  @ b-old-window-width  !
    b-window-height @ b-old-window-height !
    b-window-size   @ b-old-window-size   !
    b-window-buffer @ b-old-window-buffer ! ;

: b-get-new-window-info ( -- )
    \ get current window dimensions
    form b-window-width ! b-window-height ! 
    b-window-width @ b-window-height @ * b-window-size ! ;

: b-window-info-change? ( -- t )
    \ see if any of the window parameters have changed
    b-window-width  @ b-old-window-width  @ <>
    b-window-height @ b-old-window-height @ <> or ;

: b-window-zero-line ( tgtptr tgtwid -- )
    \ zero out buffer line (no checking)
    0 +do ( tgtptr )
        \ clear target cell
        dup 0 swap ! 
        \ advance pointer
        cell+   ( tgtptr )
    loop   ( tgtptr )
    drop ;

: b-window-copy-line ( tgtptr srcptr tgtwid srcwid -- )
    \ copy buffer line (no parameter checking!)
    \ compute diffwid = tgtwid - srcwid
    2dup - -rot      ( tgtptr srcptr diffwid tgtwid srcwid )
    \ compute minimum width
    min                     ( tgtptr srcptr diffwid minwid )
    \ copy line
    0 +do ( tgtptr srcptr diffwid )
        >r
        \ copy cell
        2dup @ swap ! ( tgtptr srcptr )
        \ advance pointers
        cell+ swap cell+ swap
        \ prepare for next iteration
        r>
    loop ( tgtptr srcptr diffwid )
    \ check if target line was larger
    dup 0> if ( tgtptr srcptr diffwid )
        nip     ( tgtptr diffwid )
        \ fill remaining target cells with zero
        b-window-zero-line
    else    ( tgtptr srcptr diffwid )
        drop 2drop
    endif ;
        
: b-cell-addr ( bufaddr x y linew -- addr )
    \ compute window buffer cell address
    * + cells + ;

: b-copy-line-old-to-new ( y -- )
    \ copy line from old to new window buffer (no y checking)
    dup ( y y )
    \ compute pointer in new buffer 
    b-window-buffer @ swap ( y newbuf y )
    0 swap                 ( y newbuf 0 y )
    b-window-width @       ( y newbuf 0 y w )
    b-cell-addr            ( y newptr )
    swap                   ( newptr y )
    \ compute pointer in old buffer
    b-old-window-buffer @ swap  ( newptr oldbuf y )
    0 swap                      ( newptr oldbuf 0 y )
    b-old-window-width @        ( newptr oldbuf 0 y w )
    b-cell-addr                 ( newptr oldptr )
    \ prepare for copying line
    b-window-width @            ( newptr oldptr newwidth )
    b-old-window-width @        ( newptr oldptr newwidth oldwidth )
    b-window-copy-line ;

: b-window-copy-old-to-new ( -- )
    \ copy old window buffer to new window buffer
    \ get new and old window height
    b-window-height @ b-old-window-height @     ( newh oldh )
    \ compute difference
    2dup - -rot                                 ( diffh newh oldh )
    \ compute minimum between old and new height
    min                                         ( diffh minh )
    \ loop over lines
    0 +do   ( diffh )
        \ copy line
        i b-copy-line-old-to-new
    loop ( diffh )
    \ check if new window height was greater
    dup 0> if ( diffh )
        \ yes: zero out remaining lines
        0 +do
            b-old-window-buffer @ 0 i b-old-window-width @  ( oldbuf 0 y w )
            b-cell-addr     ( oldptr )
            b-old-window-width @ ( oldptr w )
            b-window-zero-line
        loop
    else ( diffh )
        \ no
        drop
    endif ;

: b-window-alloc-buffer ( -- )
    \ allocate a new buffer
    b-window-size @ cells allocate throw b-window-buffer ! ;

: b-window-resize-buffer ( -- )
    \ resize the window buffer (old vs new)
    \ first check if there has been no previous buffer; in this case, do nothing
    \ otherwise, copy the old content into the new buffer
    \ allocate a new buffer
    b-window-alloc-buffer
    \ check if an old buffer exists
    b-old-window-buffer @ 0<> if
        \ buffer already existed before size change: copy old content into new buffer
        b-window-copy-old-to-new
        \ free old buffer
        b-old-window-buffer @ free throw
        0 b-old-window-buffer !
    endif ;

