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

\ === SCREEN UPDATE MANAGMENT ===========================================================

\ --- SCROLLING -------------------------------------------------------------------------

: b-scroll-buffer-up ( -- )
    \ scroll window buffer up, clear revealed line at the bottom
    \ check if window height is at least 2
    b-window-height @ 2 >= if
        \ compute target pointer at (0,0)
        b-window-buffer @           ( tgtptr )
        \ compute source pointer at (0,1)
        dup b-window-width @ cells + ( tgtptr srcptr )
        \ loop over height-1 lines
        b-window-height @ 1- 0 +do  ( tgtptr srcptr )
            \ copy line
            2dup b-window-width @ dup   ( tgtptr srcptr tgtptr srcptr w w )
            b-window-copy-line          ( tgtptr srcptr )
            \ compute offset to next line
            b-window-width @ cells      ( tgtptr srcptr offs )
            \ add to both pointers
            dup >r + swap r> + swap     ( tgtptr srcptr )
        loop ( tgtptr srcptr )
        drop ( tgtptr )
        \ fill revealed line with zeroes
        b-window-width @    ( tgtptr w )
        b-window-zero-line
    else ( h )
        drop
    endif ;

: b-scroll-buffer-down ( -- )
    \ scroll window buffer down, clear revealed line at the top
    \ check if window height is at least 2
    b-window-height @ 2 >= if
        \ compute target pointer at (0,h-1)
        b-window-buffer @ b-window-size @ b-window-width @ - cells + ( tgtptr )
        \ compute source pointer at (0,h-2)
        dup b-window-width @ cells - ( tgtptr srcptr )
        \ loop over height-1 lines
        b-window-height @ 1- 0 +do  ( tgtptr srcptr )
            \ copy line
            2dup b-window-width @ dup   ( tgtptr srcptr tgtptr srcptr w w )
            b-window-copy-line          ( tgtptr srcptr )
            \ compute offset to next line
            b-window-width @ cells      ( tgtptr srcptr offs )
            \ sub from both pointers
            dup >r - swap r> - swap     ( tgtptr srcptr )
        loop ( tgtptr srcptr )
        drop ( tgtptr )
        \ fill revealed line with zeroes
        b-window-width @    ( tgtptr w )
        b-window-zero-line
    else ( h )
        drop
    endif ;

\ --- SCREEN REFRESH --------------------------------------------------------------------

: b-xy-address  ( x y -- addr )
    \ compute buffer address for X/Y position
    b-window-buffer @ -rot      ( bufaddr x y )
    b-window-width @            ( bufaddr x y w )
    b-cell-addr ;               ( celladdr )

: b-refresh-lines ( y cnt -- )
    \ refresh entire screen
    b-attribute @ b-attribute-backup !      \ backup current attribute
    b-default-attribute @ b-attribute !     \ reset attribute to default
    \ turn off auto-wrap
    b-reset-autowrap
    \ iterate over lines ( y cnt )
    over 0 swap                 ( y cnt 0 y )
    b-xy-address                ( y cnt scraddr )
    -rot                        ( scraddr y cnt )
    over +                      ( scraddr y y+cnt )
    swap                        ( scraddr y+cnt y )
    +do 
        \ ( addr ) set cursor position to beginning of current line
        0 i at-xy
        \ ( addr ) iterate over line, output chars
        b-window-width @ 0 +do dup @ b-cell-emit cell+ loop
    loop ( addr )
    drop
    \ restore cursor position, re-enable autowrap
    b-cursor-x @ 1- b-cursor-y @ 1- at-xy b-set-autowrap 
    \ change attribute back to what it was before
    b-attribute-backup @ dup b-attribute ! b-ansi-color ;

: b-handle-refresh ( -- )
    b-clear-def
    \ refresh entire screen
    0 b-window-height @
    b-refresh-lines ;

\ --- CURSOR POSITIONING ----------------------------------------------------------------

: b-cursor-y-invalid? ( n -- t )
    \ check if cursor Y position is invalid
    dup 0<                  ( n xbeg )
    over                    ( n xbeg n )
    b-window-height @ >=    ( n xbeg xend )
    or                      ( n xrange )
    swap drop ;             ( xrange )

: b-cursor-x-invalid? ( n -- t )
    \ check if cursor X position is invalid
    dup 0<              ( n xbeg )
    over                ( n xbeg n )
    b-window-width @ >= ( n xbeg xend )
    or                  ( n xrange )
    swap drop ;         ( xrange )

: b-set-cursor ( x y -- )
    b-cursor-y ! b-cursor-x ! ;

: b-cursor-address ( -- addr )
    \ compute buffer address for cursor position
    b-cursor-x @ 1- b-cursor-y @ 1- b-xy-address ;

: b-locate-nac ( x y -- )
    \ set cursor to specified screen position (starting from 1,1)
    \ (no update check)
    1- swap 1- swap             ( x y )
    \ check X/Y position
    dup b-cursor-y-invalid?     ( x y yinval )
    rot                         ( y yinval x )
    dup b-cursor-x-invalid?     ( y yinval x xinval )
    >r                          ( y yinval x ) ( R: xinval )
    swap                        ( y x yinval ) ( R: xinval )
    r>                          ( y x yinval xinval )
    or                          ( y x invalid )
    \ if cursor position invalid: correct it
    if                          ( y x )
        dup 0< if drop 0 then   ( y x<0?0:x )
        b-window-width @ 1- >r  ( y x ) ( R: w-1 )
        dup r@ > if drop r@ then    ( y x>w-1?w-1:x ) ( R: w-1 )
        rdrop                   ( y x )
        swap                    ( x y )
        dup 0< if drop 0 then   ( x y<0?0:y )
        b-window-height @ 1- >r ( x y ) ( R: h-1 )
        dup r@ > if drop r@ then    ( x y>h-1?h-1:y ) ( R: h-1 )
        rdrop                   ( x y )
    else
        swap                    ( x y )
    endif
    \ set cursor position
    2dup                    ( x y x y )
    1+ swap 1+ swap         ( x y x+1 y+1 )
    b-set-cursor            ( x y )
    at-xy ;

\ --- LINE MEASUREMENT ------------------------------------------------------------------

: b-eol-addr ( y -- addr )
    \ compute end of line address for specified line ( no checking )
    b-window-width @ 1- swap    ( w-1 y )
    b-xy-address ;              ( celladdr )

: b-bol-addr ( y -- addr )
    \ compute beginning of line address for specified line ( no checking )
    0 swap                      ( 0 y )
    b-xy-address ;              ( celladdr )

: b-down-extent ( y -- y )
    \ get bottom-most Y position for current line
    \ the end address starts at the current cursor position and ends at the end of the buffer
    \ or the line that does have a zero cell in its rightmost column
    begin               ( y )
        \ compute cell address of last column in current line
        dup b-eol-addr          ( y celladdr )
        \ check if it does not contain zero
        @ 255 and 0<>                   ( y nonzero )
        \ check if next line would be on-screen
        over 1+ b-window-height @ <     ( y nonzero onscreen )
        \ continue loop if both of these conditions is met
        and while                       ( y )
        \ yes, can increment position and iterate loop
        1+
    repeat ;            ( yend )

: b-up-extent ( y -- y )
    \ get top-most Y position for current line
    \ the beginning address starts at the current cursor position and ends at the beginning of
    \ the buffer or the line that does have a zero cell in its rightmost column
    begin               ( y )
        \ compute anticipated Y position one line upward
        dup 1- 0< if    ( y )
            \ position would be offscreen, set loop termination flag
            -1          ( y stop )
        else            ( y )
            \ position would be on-screen, decrement for real
            1-
            \ compute cell address of last column 
            dup b-eol-addr              ( y celladdr )
            \ check if it contains zero
            @ 255 and 0=                ( y iszero )
            \ if it is zero, that means the current Y position doesn't belong to that line
            \ correct it by incrementing
            if                          ( y )
                1+                      ( y )
                \ then put a loop termination flag
                -1                      ( y stop )
            else                        ( y )
                \ otherwise, the loop can be continued
                0                       ( y stop )
            endif
        endif           ( y stop )
    until ;             ( ybeg )

: b-y-line-extent ( y -- ybeg yend )
    \ get upward and downward extent of current line, in Y positions
    dup b-up-extent swap    ( ybeg y )
    b-down-extent           ( ybeg yend ) ;

: b-y-line-width ( y -- w )
    \ get rightmost character position in current line at specified Y position + 1
    \ by counting backwards from the rightmost column 
    \ compute cell address of first column in current line
    dup b-bol-addr swap         ( boladdr y )
    \ compute cell address of last  column in current line
    b-eol-addr                  ( boladdr eoladdr )
    begin       ( boladdr eoladdr )
        \ check if current cell contains zero 
        \ if not, the current character is the last character of the line
        dup @ 255 and 0=        ( boladdr eoladdr tzero )
        \ compute next cell address
        if                      ( boladdr eoladdr )
            \ the current cell was zero, decrement character position
            cell -              ( boladdr eoladdr )
            \ check if eoladdr is now < boladdr
            2dup >              ( boladdr eoladdr stop )
        else                    ( boladdr eoladdr )
            \ stop at non-zero
            -1                  ( boladdr eoladdr stop )
        endif
    until                       ( boladdr eoladdr )
    \ increment position
    cell+
    \ convert to X position, which is the width of the line
    swap - cell / ;             ( width )

: b-right-extent ( y -- x )
    \ get last character position in line, which is one less than the width
    b-y-line-width
    \ if zero (i.e. if the line has zero width), return that,
    \ otherwise decrement the width by one to get the rightmost valid position
    dup 0<> if 1- then ;

: b-line-extent ( y -- xbeg ybeg xend yend )
    \ get line extent backwards and forward of the specified Y position
    \ first, up/down extent
    b-y-line-extent     ( ybeg yend )
    \ beginning column of line is always 0
    0 -rot              ( 0 ybeg yend )
    \ get right extent of final line
    dup b-right-extent swap ;   ( 0 ybeg xend yend )

: b-line-extent-addr ( y -- begaddr endaddr )
    \ get line extent in X/Y positions
    b-line-extent   ( xbeg ybeg xend yend )
    b-xy-address    ( xbeg ybeg endaddr )
    -rot            ( endaddr xbeg ybeg )
    b-xy-address    ( endaddr begaddr )
    swap ;          ( begaddr endaddr )

: b-bufaddr-to-y    ( bufaddr -- y )
    b-window-buffer @ - ( bufoffs )
    cell /              ( bufcell )
    b-window-width  @ / ( y ) ;

: b-bufaddr-to-poscnt   ( begaddr endaddr -- y cnt )
    swap                ( endaddr begaddr )
    b-bufaddr-to-y      ( endaddr begY )
    swap                ( y endaddr )
    b-bufaddr-to-y 1+   ( y endY+1 )
    over - ;            ( y cnt )

\ --- LINE UPDATES ----------------------------------------------------------------------

: b-line-init-addr  ( begaddr endaddr -- )
    \ initialize line on-screen (in buffer) by filling in gaps (0 bytes)
    \ (doesn't change cell attributes)
    swap                ( endaddr begaddr )
    begin               ( endaddr begaddr )
        dup @           ( endaddr begaddr val )
        dup 255 and 0=  ( endaddr begaddr val zero )
        if              ( endaddr begaddr val )
            32 or       ( endaddr begaddr val )
            over !      ( endaddr begaddr )
        else            ( endaddr begaddr val )
            drop        ( endaddr begaddr )
        endif
        cell+           ( endaddr begaddr )
        2dup <
    until               ( endaddr begaddr )
    2drop ;

: b-line-apply-attr-addr    ( attr begaddr endaddr -- )
    \ change cell attributes at specified screen buffer address range
    swap                ( attr endaddr begaddr )
    begin               ( attr endaddr begaddr )
        rot             ( endaddr begaddr attr )
        >r              ( endaddr begaddr ) ( R: attr )
        \ fetch cell contents
        dup @           ( endaddr begaddr val ) ( R: attr )
        \ mask off non-char bits
        255 and         ( endaddr begaddr val ) ( R: attr )
        \ apply attribute
        r@ or           ( endaddr begaddr val ) ( R: attr )
        over !          ( endaddr begaddr ) ( R: attr )
        r>              ( endaddr begaddr attr )
        -rot            ( attr endaddr begaddr )
        \ go to next cell
        cell+           ( attr endaddr begaddr )
        2dup <
    until               ( attr endaddr begaddr )
    2drop drop ;

: b-line-mark-addr  ( begaddr endaddr -- )
    b-mark-attribute @  ( begaddr endaddr attr )
    -rot                ( attr begaddr endaddr )
    b-line-apply-attr-addr ;

: b-line-unmark-addr  ( begaddr endaddr -- )
    b-default-attribute @   ( begaddr endaddr attr )
    -rot                    ( attr begaddr endaddr )
    b-line-apply-attr-addr ;

: b-line-init       ( y -- )
    \ initialize line on-screen (in buffer) by filling in gaps (0 bytes)
    \ (doesn't change cell attributes)
    b-line-extent-addr ( begaddr endaddr )
    b-line-init-addr ;

: b-refresh-line-addr   ( begaddr endaddr -- )
    \ refresh entire line from buffeer address range
    \ convert buffer addresses to pos/cnt
    b-bufaddr-to-poscnt     ( y cnt )
    \ refresh lines on-screen
    b-refresh-lines ;

: b-grab-line ( -- begaddr endaddr )
    \ mark current line for editing
    b-cursor-y @ 1-         ( y )
    \ get line extent as buffer addresses
    b-line-extent-addr      ( begaddr endaddr )
    \ set zero-bytes to spaces within line
    2dup b-line-init-addr ; ( begaddr endaddr )

: b-mark-line ( -- )
    b-grab-line
    \ apply hilight color attribute to line
    2dup b-line-mark-addr   ( begaddr endaddr )
    \ refresh line
    b-bufaddr-to-poscnt     ( y cnt )
    2dup b-refresh-lines
    \ update marking variables
    b-mark-cnt ! b-mark-y ! -1 b-mark-flag ! ;

: b-unmark-line ( -- )
    \ unmark line if one was marked
    b-mark-flag @ if
        \ clear mark flag
        0 b-mark-flag !
        \ get line extent of marked line
        b-mark-y @
        b-line-extent-addr  ( begaddr endaddr )
        \ clear hilight
        2dup b-line-unmark-addr
        \ refresh line
        b-refresh-line-addr
    endif ;

\ --- WINDOW CONTROL --------------------------------------------------------------------

: b-init-window ( -- )
    1 1 b-set-cursor
    b-get-new-window-info
    b-window-alloc-buffer ;

: b-update-window-size ( -- )
    \ save previous window parameters
    b-save-window-info
    \ get new text window parameters
    b-get-new-window-info
    \ check if any window parameter has changed
    b-window-info-change? if
        \ yes: resize window buffer
        b-mark-flag @       ( markflg )
        b-unmark-line       ( markflg )
        b-window-resize-buffer ( markflg )
        \ attempt to set cursor at last known position
        b-cursor-x @ b-cursor-y @ b-locate-nac      ( markflg )
        \ if a mark was set, check Y position
        if
            b-mark-y @ b-window-height @ >= if
                \ beyond screen: clear mark flag
                0 b-mark-flag !
            else
                \ within screen boundaries: try to reinstate mark
                b-mark-line
            endif
        endif
        b-handle-refresh
    endif ;

: b-update-window-size? ( -- t )
    \ check if window dimensions have changed
    form b-window-width <> b-window-height <> or ;

: b-auto-update-window ( -- )
    \ check if window update is necessary, and do so if so
    b-update-window-size? if b-update-window-size then ;

\ --- CONSOLE SERVICE ROUTINES ----------------------------------------------------------

: b-locate ( x y -- )
    \ set cursor to specified screen position (starting from 1,1)
    b-locate-nac ;

: b-cls ( -- )
    \ clear screen and set cursor to top left screen position
    b-clear 1 1 b-locate b-clear-buffer ;

: b-scroll-up ( -- )
    b-CSI ." S" b-scroll-buffer-up ;

: b-scroll-down ( -- )
    b-CSI ." T" b-scroll-buffer-down ;

: b-anticipate-cursor-up-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor up had been pressed
    \ sub 1 from y 
    1- 
    \ check if window height has been exceeded
    dup 0<= if
        \ yes: add 1 to y
        1+
        \ scroll down
        b-scroll-down   
    endif ;

: b-anticipate-cursor-up ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor up had been pressed
    b-anticipate-cursor-up-event ;

: b-anticipate-cursor-down-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor down had been pressed
    \ add 1 to y 
    1+ 
    \ check if window height has been exceeded
    dup b-window-height @ > if
        \ yes: sub 1 from y
        1-
        \ scroll up
        b-scroll-up 
    endif ;

: b-anticipate-cursor-down ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor down had been pressed
    b-anticipate-cursor-down-event ;

: b-anticipate-return-event ( x y -- x y )
    \ recompute anticipated cursor position as if return had been pressed
    \ reset x to 1
    swap drop 1 swap
    \ move anticipated cursor down
    b-anticipate-cursor-down-event ;

: b-anticipate-return ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if return had been pressed
    b-anticipate-return-event ;

: b-anticipate-next-char ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ add 1 to x and check if window width has been exceeded
    over 1+ b-window-width @ > if
        \ yes: recompute anticipated cursor position as if return had been pressed
        b-anticipate-return-event
    else
        \ no: add 1 to x for real
        swap 1+ swap
    endif ;

: b-output-return ( -- )
    \ output a newline
    \ get anticipated cursor position
    b-anticipate-return ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-anticipate-cellar-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor moved backwards
    \ to the right hand edge of the screen
    \ reset x to b-window-width
    swap drop b-window-width @ swap
    \ move anticipated cursor up
    b-anticipate-cursor-up-event ;

: b-anticipate-cellar ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor appeared at eol
    b-anticipate-cellar-event ;

: b-anticipate-prev-char ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ sub 1 from x and check if window width has been exceeded
    over 1- 0<= if
        \ yes: recompute anticipated cursor position as if cursor appeared at eol
        b-anticipate-cellar-event
    else
        \ no: sub 1 from x for real
        swap 1- swap
    endif ;

: b-emit ( chr -- )
    \ emit character and store into window buffer
    \ remove unwanted bits and set attribute bits
    255 and b-attribute @ or
    \ ( chr )
    dup
    \ ( chr chr ) compute buffer position
    b-cursor-address
    \ ( chr chr addr ) store character info into cell
    ! 
    \ compute anticipated cursor position
    b-anticipate-next-char
    \ ( chr x y ) output character for real
    2 pick b-cell-emit
    \ ( chr x y ) locate to anticipated position
    b-locate
    \ ( chr )
    drop ;

: b-type ( addr u -- )
    \ output 'u' characters from address 'addr'
    0 u+do dup c@ b-emit char+ loop drop ;

: b-emitnum ( n -- )
    \ output decimal number (without surrounding blanks)
    dup 0= if 
        \ if the value is zero, only output that
        drop bc-digit-lo b-emit
    else
        \ if the number is negative, output minus sign and negate
        dup 0< if
            bc-minus b-emit
            negate
        endif
        \ divide number by 10
        dup 10 /
        \ if non-zero, recurse with that number
        dup 0<> if
            recurse
        else
            drop
        endif
        \ output least significant digit of decimal number
        10 mod bc-digit-lo + b-emit
    endif ;

: b-ansi-color ( attr -- )
    \ output ANSI color sequence for specified attributes
    b-split-attr rot    ( bgcol fgcol mode )
    dup case ( mode )
        0 of  
            \ mode 0 deactivates attributes (normal mode): keep
        endof
        1 of   
            \ mode 1 puts it in bold face / high intesity : keep
        endof
        2 of   
            \ mode 2 makes it blink : in ANSI, 5
            drop 5
        endof
        3 of   
            \ mode 3 reverse video : in ANSI, 7
            drop 7
        endof
    endcase
    b-CSI 
    ( bgcol fgcol mode )
    b-outnum b-SEMIC        \ output new mode
    30 + b-outnum b-SEMIC   \ set foreground color
    40 + b-outnum           \ set background color
    ." m" ;

