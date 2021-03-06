// file: mysh/src/parse.c
// by  : jooh@cuni.cz
// for : nswi015
// lic.: mit

////////////////////////////////////////////////////////////////////////
// directives & globals

#include <dirent.h>   /* DIR, {close,open}dir */
#include <fcntl.h>    /* open */
#include <regex.h>    /* reg{comp,exec,free} */
#include <sys/wait.h> /* wait */
#include "mysh.h"     /* mysh.h includes:
                       * stdio, stdlib, editline/readline, errno,
                       * histedit, string, sys/queue */

extern int errno;

////////////////////////////////////////////////////////////////////////
// parse, eval, et al.

/*
 * check
 * - Check (user inputted) line sanity using regex.
 */
int
check(const char* line)
{
    regex_t re;

    if ((regcomp(&re, "[;|<][ \t]*[;|<]", 0)) != 0)
    {
        warnx("regex compilation error");
        return 1;
    }
    if ((regexec(&re, line, 0, NULL, 0)) == 0)
    {
        regfree(&re);
        warnx("%d: syntax error", LINECOUNT);
        LASTOK = ERR_SYN;
        return -1;
    }
    regfree(&re);

    if ((regcomp(&re, ">>>", 0)) != 0)
    {
        warnx("regex compilation error");
        return 1;
    }
    if ((regexec(&re, line, 0, NULL, 0)) == 0)
    {
        regfree(&re);
        warnx("%d: syntax error", LINECOUNT);
        LASTOK = ERR_SYN;
        return -1;
    }
    regfree(&re);

    if ((regcomp(&re, "^[ \t]*[|<>]", 0)) != 0)
    {
        warnx("regex compilation error");
        return 1;
    }
    if ((regexec(&re, line, 0, NULL, 0)) == 0)
    {
        regfree(&re);
        warnx("%d: syntax error", LINECOUNT);
        LASTOK = ERR_SYN;
        return -1;
    }
    regfree(&re);

    if ((regcomp(&re, "[|<>][ \t\n]*$", 0)) != 0)
    {
        warnx("regex compilation error");
        return 1;
    }
    if ((regexec(&re, line, 0, NULL, 0)) == 0)
    {
        regfree(&re);
        warnx("%d: syntax error", LINECOUNT);
        LASTOK = ERR_SYN;
        return -1;
    }
    regfree(&re);

    return 0;
}

/*
 * fill_words
 * - Helper to parse()'s structure-filling. This is at depth level 3.
 */
int
fill_words(char* str, struct ltok* fill)
{
    struct ltok* words;
    char* w;
    char* w_cp;
    char* w_end = NULL;

    w = strtok_r(str, " \t", &w_end);
    while (w != NULL)
    {
        words = malloc(sizeof(struct ltok));
        if (words == NULL)
        {
            warnx("malloc error");
            return 1;
        }

        w_cp = malloc(sizeof(char) * LIM);
        if (w_cp == NULL)
        {
            warnx("malloc error");
            return 1;
        }
        if (strlen(w) > LIM)
        {
            warnx("input truncated");
        }

        strcpy(w_cp, w);
        if (w_cp)
        {
          trim(&w_cp);
        }
        if (white(w_cp))
        {
            free(w_cp);
            w = strtok_r(NULL, " \t", &w_end);
            continue;
        }

        words->word = w_cp;
        TAILQ_INSERT_TAIL(&fill->head, words, list);

        w = strtok_r(NULL, " \t", &w_end);
    }

    return 0;
}

/*
 * fill_cmds
 * - Helper to parse()'s structure-filling. This is at depth level 2.
 */
int
fill_cmds(char* str, struct lltok* fill)
{
    struct lltok* cmds;
    struct ltok* cmd;
    char* c;
    char* c_cp;
    char* c_end = NULL;
    char* c_new;
    int ok;
    int red;
    char* red_r;
    char* red_w;
    char* red_a;

    c = strtok_r(str, "|", &c_end);
    while (c != NULL && !white(c))
    {
        red_r = NULL;
        red_w = NULL;
        red_a = NULL;

        if (redir('r', c, &red_r) < -1)
        {
            return 1;
        }
        ok = redir('w', c, &red_w);
        if (ok < -1)
        {
            return 1;
        }
        red = redir('a', c, &red_a);
        if (red < -1)
        {
            return 1;
        }
        if (red > ok)
        {
            red_w = NULL;
        }
        else
        {
            red_a = NULL;
        }
        if (red_r != NULL)
        {
            if (access(red_r, R_OK) == -1)
            {
                warnx("redirected read file does not exist");
                return 0;
            }
        }

        cmds = malloc(sizeof(struct lltok));
        if (cmds == NULL)
        {
            warnx("malloc error");
            return 1;
        }

        /* remove any redirection */
        c_new = unred(c);

        c_cp = malloc(sizeof(char) * LIM);
        if (c_cp == NULL)
        {
            warnx("malloc error");
            return 1;
        }
        if (strlen(c_new) > LIM)
        {
            warnx("input truncated");
        }
        strcpy(c_cp, c_new);
        free(c_new);

        cmd = malloc(sizeof(struct ltok));
        if (cmd == NULL)
        {
            warnx("malloc error");
            return 1;
        }
        TAILQ_INIT(&cmd->head);

        if ((fill_words(c_cp, cmd)) != 0)
        {
            free(c_cp);
            return 1;
        }

        cmds->cmd = cmd;
        cmds->red_r = red_r;
        cmds->red_w = red_w;
        cmds->red_a = red_a;
        TAILQ_INSERT_TAIL(&fill->head, cmds, list);

        free(c_cp);
        c = strtok_r(NULL, "|", &c_end);
    }

    return 0;
}

/*
 * fill_semis
 * - Helper to parse()'s structure-filling. This is at depth level 1.
 */
int
fill_semis(char* str, struct llltok* fill)
{
    struct llltok* semis;
    struct lltok* semi;
    char* s;
    char* s_cp;
    char* s_end = NULL;

    s = strtok_r(str, ";", &s_end);
    while (s != NULL && !white(s))
    {
        semis = malloc(sizeof(struct llltok));
        if (semis == NULL)
        {
            warnx("malloc error");
            return 1;
        }

        s_cp = malloc(sizeof(char) * LIM);
        if (s_cp == NULL)
        {
            warnx("malloc error");
            return 1;
        }
        if (strlen(s) > LIM)
        {
            warnx("input truncated");
        }
        strcpy(s_cp, s);

        semi = malloc(sizeof(struct lltok));
        if (semi == NULL)
        {
            warnx("malloc error");
            return 1;
        }
        TAILQ_INIT(&semi->head);

        if ((fill_cmds(s_cp, semi)) != 0)
        {
            free(s_cp);
            return 1;
        }

        semis->semi = semi;
        TAILQ_INSERT_TAIL(&fill->head, semis, list);

        free(s_cp);
        s = strtok_r(NULL, ";", &s_end);
    }

    return 0;
}

/*
 * parse
 * - Split input on semicolon, pipe, and whitespace, in that order.
 *   Accomplish this by using 3 nested loops. See fill_*().
 */
int
parse(char* line, struct llltok* all)
{
    /* ignore comments */
    char* p = strstr(line, "#");

    if (p != NULL)
    {
        *p = '\0';
    }

    /* nested structuring of syntactic components */
    if ((fill_semis(line, all)) != 0)
    {
        return 1;
    }

    return 0;
}

/*
 * child_sigint_handler
 * - Handle C-c for children.
 */
void
child_sigint_handler(int sig)
{
    warnx("Killed by signal %d", sig);
    LASTOK = ERR_SIG + sig;
}

/*
 * spawn
 * - Fork a given command while handling pipes and redirects.
 */
int
spawn(char** cmd, int in, int first, int last,
      const char* red_r, const char* red_w, const char* red_a)
{
    struct sigaction sa;

    sa.sa_handler = child_sigint_handler;
    sa.sa_flags = 0;
    sigemptyset(&sa.sa_mask);
    sigaction(SIGINT, &sa, NULL);

    int p[2];

    if (pipe(p) == -1)
    {
        warnx("pipe error");
        return -1;
    }

    int status = 0;
    pid_t child = fork();

    if (child < 0)
    {
        warnx("fork error");
        return -1;
    }

    if (child == 0)
    {
        /* piping */
        if (in != -1) /* this cmd is piped */
        {
            if (first == 1) /* first in pipe */
            {
                dup2(p[1], 1);
            }
            else if (first == 0 && last == 0) /* middle of pipe */
            {
                dup2(in, 0);
                dup2(p[1], 1);
            }
            else /* last of pipe */
            {
                dup2(in, 0);
            }
        }

        /* redirection */
        int f = -1;

        if (red_r != NULL)
        {
            if ((f = open(red_r, O_RDONLY, 0)) < 0)
            {
                warnx("redirected read error");
                return -1;
            }
            dup2(f, 0);
            close(f);
        }

        if (red_w != NULL)
        {
            if ((f = open(red_w, O_WRONLY | O_CREAT | O_TRUNC,
                          S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH)) < 0)
            {
                warnx("redirected write error");
                return -1;
            }
            dup2(f, 1);
            close(f);
        }

        if (red_a != NULL)
        {
            if ((f = open(red_a, O_APPEND | O_WRONLY | O_CREAT,
                          S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH)) < 0)
            {
                warnx("redirected append error");
                return -1;
            }
            dup2(f, 1);
            close(f);
        }

        /* actual exec */
        if (execvp(cmd[0], cmd) < 0)
        {
            warnx("%s: command not found", cmd[0]);
            exit(ERR_EXEC); /* exit forked child */
        }
        LASTOK = errno;
    }

    else
    {
        while (wait(&status) != child);
    }

    if (WIFEXITED(status))
    {
        LASTOK = WEXITSTATUS(status);
        if (LASTOK == ERR_EXEC)
        {
            return -1;
        }
    }

    if (in != -1) /* this cmd is piped */
    {
      if (last == 1)
      {
          close(p[0]);
      }
      if (in != 0)
      {
          close(in);
      }
      close(p[1]);
    }

    return p[0];
}

/*
 * eval
 * - Evaluate result of parse.
 * - Converts llltok into behavior (ie. system calls or native
 *   functionality).
 */
int
eval(const struct llltok* semis)
{
    struct llltok* semi;
    struct lltok* cmd;
    struct ltok* word;
    int cmd_count;
    int spawn_count;
    int count;
    char* r;
    char* w;
    char* a;
    int in = 0;
    int first = 1;

    TAILQ_FOREACH(semi, &semis->head, list)
    {
        cmd_count = 0;
        TAILQ_FOREACH(cmd, &semi->semi->head, list)
        {
            ++cmd_count;
        }

        spawn_count = 0;
        in = 0;
        first = 1;
        TAILQ_FOREACH(cmd, &semi->semi->head, list)
        {
            /* build cmd as an array */
            count = 0;
            TAILQ_FOREACH(word, &cmd->cmd->head, list)
            {
                ++count;
            }

            char* c[count + 1];
            count = 0;
            TAILQ_FOREACH(word, &cmd->cmd->head, list)
            {
                c[count++] = word->word;
            }
            c[count] = NULL;

            /* redirects */
            r = cmd->red_r;
            w = cmd->red_w;
            a = cmd->red_a;

            /* cd */
            if (strcmp("cd", c[0]) == 0)
            {
                cd(c, count);
                continue;
            }

            /* exit */
            if (strcmp("exit", c[0]) == 0)
            {
                return 1;
            }

            /* exec */
            if (cmd_count < 2) /* no pipeline */
            {
                if (spawn(c, -1, -1, -1, r, w, a) == -1)
                {
                    return -1;
                }
            }
            else /* pipeline */
            {
                /* not last in pipeline */
                ++spawn_count;
                if (spawn_count < cmd_count)
                {
                    if ((in = spawn(c, in, first, 0, r, w, a)) == -1)
                    {
                        return -1;
                    }
                    first = 0;
                }
                else /* last in pipeline */
                {
                    if ((in = spawn(c, in, 0, 1, r, w, a)) == -1)
                    {
                        return -1;
                    }
                }
            }

            struct sigaction sa;

            sa.sa_handler = sigint_handler;
            sa.sa_flags = 0;
            sigemptyset(&sa.sa_mask);
            sigaction(SIGINT, &sa, NULL);
        }
    }

    return 0;
}

/*
 * redir
 * - Detect read/write/append redirection ("<" / ">" / ">>"
 *   respectively), returning the string (as argument) and the index at
 *   which the redirection occurs (as return value).
 */
int
redir(char type, char* line, char** s)
{
    char sign = '>';
    unsigned long signlen = 1;
    int found = 0;
    int count = 0;
    int where = -1;
    char* word_end = NULL;
    char* word;
    char* word_cp;
    char* line_cp;

    line_cp = malloc(sizeof(char) * LIM);
    if (line_cp == NULL)
    {
        warnx("malloc error");
        return -2;
    }
    if (strlen(line) > LIM)
    {
        warnx("input truncated");
    }
    strcpy(line_cp, line);

    if (type == 'r')
    {
        sign = '<';
    }
    if (type == 'a')
    {
        signlen = 2;
    }

    word = strtok_r(line_cp, " \t", &word_end);
    while (word != NULL)
    {
        ++count;
        if (word[0] == sign                      // a redirection symbol
            && (type != 'a' || word[1] == sign)  // if 'a' then ">>"
            && (type != 'w' || word[1] != sign)) // if 'w' then not ">>"
        {
            if (strlen(word) == signlen)
            {
                found = 1; // fast-forward to next word
            }
            else
            {
                word_cp = malloc(sizeof(char) * LIM);
                if (word_cp == NULL)
                {
                    warnx("malloc error");
                    free(line_cp);
                    return -2;
                }
                if (strlen(word) > LIM)
                {
                    warnx("input truncated");
                }
                strcpy(word_cp, word);

                /* consider only the last */
                if (s)
                {
                    free(*s);
                }

                *s = word_cp;
                memmove(*s, *s + 1, strlen(*s));
                trim(s);
                if (type == 'a')
                {
                    memmove(*s, *s + 1, strlen(*s));
                }
                where = count;
            }
        }

        word = strtok_r(NULL, " \t", &word_end);
        if (!word)
        {
            found = 0;
            continue;
        }

        if (found == 1)
        {
            word_cp = malloc(sizeof(char) * LIM);
            if (word_cp == NULL)
            {
                warnx("malloc error");
                free(line_cp);
                return -2;
            }
            if (strlen(word) > LIM)
            {
                warnx("input truncated");
            }
            strcpy(word_cp, word);

            /* consider only the last */
            if (s)
            {
                free(*s);
            }

            *s = word_cp;
            trim(s);
            where = count + 1;
        }

        found = 0;
    }
    free(line_cp);

    return where;
}

////////////////////////////////////////////////////////////////////////
// native behavior

/*
 * cd
 * - Implementation of shell-standard "cd".
 * - No arg goes home; "-" arg goes to prev; "FOO" arg goes to FOO.
 */
int
cd(char** args, int size)
{
    if (size > 2)
    {
        warnx("cd: too many arguments");
        LASTOK = ERR_FAIL;
        return 1;
    }

    char* dst;

    if (size == 1)
    {
        dst = getenv("HOME");
    }
    else
    {
        dst = args[1];
    }
    if (strcmp("-", dst) == 0)
    {
        dst = getenv("OLDPWD");
    }

    DIR* dir = opendir(dst);

    if (dir)
    {
        closedir(dir);
    }
    else if (errno == ENOENT)
    {
        warnx("cd: no such directory");
        return 1;
    }
    else
    {
        warnx("cd: opendir() error");
        return 1;
    }

    setenv("OLDPWD", getenv("PWD"), 1);
    chdir(dst);
    char abs_dst[LIM];
    getcwd(abs_dst, sizeof(abs_dst)); /* assume no error */
    setenv("PWD", abs_dst, 1);

    return 0;
}

