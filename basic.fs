
13 constant bc-cr
10 constant bc-lf
27 constant bc-escape
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

variable b-window-width
variable b-window-height
variable b-window-size
variable b-window-buffer
variable b-cursor-x
variable b-cursor-y
variable b-attribute
variable b-default-attribute
variable b-attribute-backup

variable b-old-window-width
variable b-old-window-height
variable b-old-window-size
variable b-old-window-buffer

variable b-quit-flag

: >b-attr-fg-col ( col -- attr )
    \ convert color to attribute bits
    bc-fg-vmsk and bc-fg-vshf lshift ;

: >b-attr-bg-col ( col -- attr )
    \ convert color to attribute bits
    bc-bg-vmsk and bc-bg-vshf lshift ;

: >b-attr-mode   ( mode -- attr )
    \ convert mode to attribute bits
    bc-mode-vmsk and bc-mode-vshf lshift ;

: b-attr-fg-col>    ( attr -- col )
    \ convert attribute bits to color
    bc-fg-vshf rshift bc-fg-vmsk and ;

: b-attr-bg-col>    ( attr -- col )
    \ convert attribute bits to color
    bc-bg-vshf rshift bc-bg-vmsk and ;

: b-attr-mode>      ( attr -- mode )
    \ convert attribute bits to mode
    bc-mode-vshf rshift bc-mode-vmsk and ;

: b-make-attr       ( mode bgcol fgcol -- attr )
    \ make combined attribute bits from color/mode info
    >b-attr-fg-col -rot ( fgattr mode bgcol )
    >b-attr-bg-col -rot ( bgattr fgattr mode )
    >b-attr-mode        ( bgattr fgattr modeattr )
    or or ;             ( attr )

: b-split-attr      ( attr -- mode bgcol fgcol )
    \ get separate color/mode info from combined attribute bits
    dup b-attr-mode> swap    ( mode attr )
    dup b-attr-bg-col> swap  ( mode bgcol attr )
    b-attr-fg-col> ;         ( mode bgcol fgcol )

: b-attr-fg-col+    ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    1+
    b-make-attr ;   ( attr )

: b-attr-bg-col+    ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    swap 1+ swap
    b-make-attr ;   ( attr )

: b-attr-mode+      ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    rot 1+ -rot
    b-make-attr ;   ( attr )

: b-NEWLINE
    \ output newline character(s)
    bc-cr emit bc-lf emit ;

: b-ESC ( -- )
    \ output escape character
    bc-escape emit ;

: b-CSI ( -- )
    \ output control sequence introducer ( ESC [ )
    b-ESC ." [" ;

: b-SEMIC ( -- ) 
    \ output semicolon
    ." ;" ;

: b-set-autowrap ( -- )
    b-CSI ." ?7h" ;

: b-reset-autowrap ( -- )
    b-CSI ." ?7l" ;

: b-clear-buffer ( -- )
    \ clear text buffer
    b-window-buffer @ b-window-size @ ( addr count )
    0 +do ( addr )
        0 over !
        cell+
    loop drop ;

: b-outnum ( n -- )
    \ output decimal number (without surrounding blanks)
    dup 0= if 
        \ if the value is zero, only output that
        drop ." 0"
    else
        \ if the number is negative, output minus sign and negate
        dup 0< if
            ." -"
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
        10 mod bc-digit-lo + emit
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

: b-ansi-undo ( attr -- )
    \ output ANSI undo sequence for specified attribute
    b-split-attr rot    ( bgcol fgcol mode )
    dup case ( mode )
        0 of  
            \ mode 0 deactivates attributes (normal mode): keep
        endof
        1 of   
            \ mode 1 puts it in bold face / high intesity : keep
            drop 22     \ undo bold/faint
        endof
        2 of   
            \ mode 2 makes it blink : in ANSI, 5
            drop 25     \ undo blink
        endof
        3 of   
            \ mode 3 reverse video : in ANSI, 7
            drop 27     \ undo reverse
        endof
    endcase
    b-CSI 
    b-outnum b-SEMIC    \ output new mode
    ( bgcol fgcol )     \ ignore color
    2drop
    ." m" ;

: b-clear ( -- )
    \ clear screen and set cursor to top left screen position (raw)
    b-ESC ." c" 
    b-attribute @ b-ansi-color ;

: b-output-attr ( attr -- )
    \ output attribute
    \ mask off unwanted bits
    0xff00 and ( attr )
    \ see if attribute is zero
    dup 0= if
        \ yes: use default attribute
        drop b-default-attribute @
    endif
    \ see if attribute has changed
    dup b-attribute @ <> if     \ if attribute has changed
        \ yes: ( attr )
        dup bc-mode-mask and    \ mask off non-mode bits in new attribute
        b-attribute @           \ get previous attribute
        bc-mode-mask and        \ mask off non-mode bits 
        <> if                   \ if mode has changed
            b-attribute @
            b-ansi-undo         \ undo old mode
        then
        dup b-attribute !       \ remember new attribute
        b-ansi-color            \ output that
    else
        \ no: ignore
        drop
    endif ;

: b-cell-emit ( chr -- )
    \ low-level cell emitter
    dup b-output-attr \ output attribute
    255 and     \ remove attribute bits
    dup 0= if   \ if zero, convert to space ' '
        drop 32
    then
    emit ;

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

: b-window-resize-buffer ( -- )
    \ resize the window buffer (old vs new)
    \ first check if there has been no previous buffer; in this case, do nothing
    \ otherwise, copy the old content into the new buffer
    \ allocate a new buffer
    b-window-size @ cells allocate throw b-window-buffer !
    \ check if an old buffer exists
    b-old-window-buffer @ 0<> if
        \ buffer already existed before size change: copy old content into new buffer
        b-window-copy-old-to-new
        \ free old buffer
        b-old-window-buffer @ free throw
        0 b-old-window-buffer !
    endif ;

: b-update-window-size ( -- )
    \ save previous window parameters
    b-save-window-info
    \ get new text window parameters
    b-get-new-window-info
    \ check if any window parameter has changed
    b-window-info-change? if
        \ yes: resize window buffer
        b-window-resize-buffer
    endif ;

: b-update-window-size? ( -- t )
    \ check if window dimensions have changed
    form b-window-width <> b-window-height <> or ;

: b-handle-refresh ( -- )
    \ refresh entire screen
    b-attribute @ b-attribute-backup !      \ backup current attribute
    b-default-attribute @ b-attribute !     \ reset attribute to default
    \ clear screen, turn off auto-wrap
    b-clear b-reset-autowrap
    \ iterate over lines
    b-window-buffer @ b-window-height @ 0 +do 
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

: b-auto-update-window ( -- )
    \ check if window update is necessary, and do so if so
    b-update-window-size? if b-update-window-size then ;

: b-cursor-y-invalid? ( n -- t )
    \ check if cursor Y position is invalid
    dup 0< over b-window-height >= or nip ;

: b-cursor-x-invalid? ( n -- t )
    \ check if cursor X position is invalid
    dup 0< over b-window-width >= or nip ;

: b-set-cursor ( x y -- )
    b-cursor-y ! b-cursor-x ! ;

: b-not ( n -- ~n )
    invert ;

: b-locate ( x y -- )
    \ set cursor to specified screen position (starting from 1,1)
    b-auto-update-window
    ( x y -- x-1 y-1 )
    1- swap 1- swap
    ( x y -- x y t )
    dup b-cursor-y-invalid?
    ( x y t -- x y t t )
    2 pick b-cursor-x-invalid?
    ( x y t t -- ) 
    or b-not if 2dup 1+ swap 1+ swap b-set-cursor at-xy then ;

: b-cls ( -- )
    \ clear screen and set cursor to top left screen position
    b-clear 1 1 b-locate b-clear-buffer ;

: b-xy-address  ( x y -- addr )
    \ compute buffer address for X/Y position
    b-window-buffer @ -rot      ( bufaddr x y )
    b-window-width @            ( bufaddr x y w )
    b-cell-addr ;               ( celladdr )

: b-cursor-address ( -- addr )
    \ compute buffer address for cursor position
    b-cursor-x @ 1- b-cursor-y @ 1- b-xy-address ;

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

: b-line-init       ( y -- )
    \ initialize line on-screen (in buffer) by filling in gaps (0 bytes)
    \ (doesn't change cell attributes)
    b-line-extent-addr ( begaddr endaddr )
    b-line-init-addr ;

: b-mark-line ( -- )
    \ mark current line for editing
    b-cursor-y @ 1-     ( y )
    ;

: b-handle-page-up ( -- )
    ;

: b-handle-page-down ( -- )
    ;

: b-handle-home ( -- )
    ;

: b-handle-end ( -- )
    ;

: b-handle-insert ( -- )
    ;

: b-handle-delete ( -- )
    ;

: b-handle-f1 ( -- )
    ;

: b-handle-f2 ( -- )
    \ F2 changes the foreground color
    b-attribute @ ( attr )
    b-attr-fg-col+
    b-output-attr ;

: b-handle-f3 ( -- )
    \ F3 changes the background color
    b-attribute @ ( attr )
    b-attr-bg-col+
    b-output-attr ;

: b-handle-f4 ( -- )
    \ F4 changes the text mode
    b-attribute @ ( attr )
    b-attr-mode+
    b-output-attr ;

: b-handle-f5 ( -- )
    ;

: b-handle-f6 ( -- )
    ;

: b-handle-f7 ( -- )
    ;

: b-handle-f8 ( -- )
    ;

: b-handle-f9 ( -- )
    ;

: b-handle-f10 ( -- )
    ;

: b-handle-f11 ( -- )
    ;

: b-handle-f12 ( -- )
    \ set quit flag
    bc-true b-quit-flag ! ;

: b-handle-escape ( -- )
    bye ;

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

: b-handle-cursor-up ( -- )
    \ get anticipated cursor position
    b-anticipate-cursor-up ( -- x y )
    \ locate to anticipated position
    b-locate ;

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

: b-handle-cursor-down ( -- )
    \ get anticipated cursor position
    b-anticipate-cursor-down ( -- x y )
    \ locate to anticipated position
    b-locate ;

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

: b-handle-cursor-right ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-anticipate-next-char ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-handle-return ( -- )
    \ return key has been pressed
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

: b-handle-cursor-left ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-handle-backspace ( -- )
    \ backspace key has been pressed
    \ get anticipated cursor position
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    2dup b-locate 
    \ rub out character under cursor
    32 emit
    \ locate to anticipated position again
    b-locate ;

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

: b-input-handler ( -- )
    key? if
        ekey ekey>char if ( c ) 
            case
                12      of b-handle-refresh endof
                13      of b-handle-return endof
                27      of b-handle-escape endof
                127     of b-handle-backspace endof
                ( c ) \ default handling:
                dup 32 < over 126 > or if ( c )
                else ( c )
                    \ printable character: first check if window size has changed
                    b-auto-update-window
                    \ compute anticipated cursor position
                    b-anticipate-next-char ( -- x y )
                    \ output character
                    2 pick b-emit
                    \ locate to anticipated position
                    b-locate
                endif
            endcase

        else ekey>fkey if ( key-id )
            case
                \ cursor keys
                k-up    of b-handle-cursor-up       endof
                k-down  of b-handle-cursor-down     endof
                k-left  of b-handle-cursor-left     endof
                k-right of b-handle-cursor-right    endof
                \ pageup / pagedown
                k-prior of b-handle-page-up         endof
                k-next  of b-handle-page-down       endof
                \ home / end
                k-home  of b-handle-home            endof
                k-end   of b-handle-end             endof
                \ insert / delete
                k-insert of b-handle-insert         endof
                k-delete of b-handle-delete         endof
                \ function keys
                k-f1    of b-handle-f1              endof
                k-f2    of b-handle-f2              endof
                k-f3    of b-handle-f3              endof
                k-f4    of b-handle-f4              endof
                k-f5    of b-handle-f5              endof
                k-f6    of b-handle-f6              endof
                k-f7    of b-handle-f7              endof
                k-f8    of b-handle-f8              endof
                k-f9    of b-handle-f9              endof
                k-f10   of b-handle-f10             endof
                k-f11   of b-handle-f11             endof
                k-f12   of b-handle-f12             endof
            endcase
        else ( keyboard-event )
            drop

        then then
    then ;

: b-screen-editor ( -- )
    \ BASIC screen editor
    begin 
        b-input-handler 
    b-quit-flag @ until ;

bc-def-mode bc-def-bgcol bc-def-fgcol b-make-attr dup b-attribute ! b-default-attribute !
b-update-window-size
b-cls
s" Forth BASIC v0.1 - Copyright (c) Ekkehard Morgenstern. All rights reserved." b-type
b-handle-return
s" Licensable under the GNU General Public License (GPL) v3 or higher." b-type
b-handle-return
s" Written for use with GNU Forth (aka GForth)." b-type
b-handle-return
b-handle-return
b-handle-refresh
b-screen-editor

