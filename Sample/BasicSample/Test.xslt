<?xml version="1.0" encoding="UTF-8" ?>
<stylesheet version="1.0" xmlns="http://www.w3.org/1999/XSL/Transform">
<output method="text"  encoding="utf-8" indent="yes"></output>
    <template match="IFs">
        using System;
        using System.Collections.Generic;
        using System.Text;

        namespace macroTest
        {
            public partial class Test
            {
                private void DoIt(int condition)
                {
                    <apply-templates select="//DOIT"></apply-templates>
                }
            }
        }
    </template>

    <template match="//DOIT">
                    if (condition == <value-of select="@condition"/>)
                    {
                        funcName_<value-of select="@condition"/>();
                    }
    </template>
</stylesheet>