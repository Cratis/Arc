import{j as n}from"./jsx-runtime-Cf8x2fCZ.js";import{S as e,a as x,b as s,c as r,d as I}from"./StoryContainer-B2MOqGRO.js";import"./index-yBjzXJbu.js";const A={title:"Stories/StoryContainer",component:e,parameters:{docs:{description:{component:"Container components for wrapping Storybook stories with consistent styling that automatically adapts to dark/light mode."}}}},i={render:()=>n.jsxs(e,{children:[n.jsx("h1",{children:"Story Container Example"}),n.jsx("p",{children:"This is a basic story container with default medium size (1200px max width). It provides consistent padding and centering."}),n.jsx("p",{children:"The container automatically adapts to both dark and light modes using CSS variables. Try switching the background in the toolbar above!"})]})},t={render:()=>n.jsxs(n.Fragment,{children:[n.jsxs(e,{size:"sm",children:[n.jsx("h2",{children:"Small Container"}),n.jsx("p",{children:"This container has a max-width of 600px, perfect for focused content."})]}),n.jsx(x,{}),n.jsxs(e,{size:"md",children:[n.jsx("h2",{children:"Medium Container (Default)"}),n.jsx("p",{children:"This container has a max-width of 1200px, suitable for most stories."})]}),n.jsx(x,{}),n.jsxs(e,{size:"lg",children:[n.jsx("h2",{children:"Large Container"}),n.jsx("p",{children:"This container has a max-width of 1400px, for wider layouts."})]})]})},a={render:()=>n.jsxs(e,{asCard:!0,children:[n.jsx("h2",{children:"Card Container"}),n.jsx("p",{children:"This container is rendered as a card with a background, border, and shadow. Perfect for highlighting content or creating distinct sections."}),n.jsx("p",{children:"Notice how the card adapts to the current theme."})]})},d={render:()=>n.jsxs(e,{children:[n.jsx("h1",{children:"Using Sections"}),n.jsxs(s,{children:[n.jsx("h2",{children:"First Section"}),n.jsx("p",{children:"Sections provide consistent vertical spacing between groups of content."})]}),n.jsxs(s,{children:[n.jsx("h2",{children:"Second Section"}),n.jsx("p",{children:"Each section automatically has margin-bottom spacing."})]}),n.jsxs(s,{children:[n.jsx("h2",{children:"Third Section"}),n.jsx("p",{children:"The last section has no bottom margin to prevent extra space."})]})]})},o={render:()=>n.jsxs(e,{children:[n.jsx("h1",{children:"Grid Layout"}),n.jsx("p",{children:"Use StoryGrid for responsive grid layouts:"}),n.jsxs(I,{children:[n.jsxs("div",{className:"story-card",children:[n.jsx("h3",{children:"Card 1"}),n.jsx("p",{children:"Grid items automatically wrap based on available space."})]}),n.jsxs("div",{className:"story-card",children:[n.jsx("h3",{children:"Card 2"}),n.jsx("p",{children:"Minimum width of 300px per item."})]}),n.jsxs("div",{className:"story-card",children:[n.jsx("h3",{children:"Card 3"}),n.jsx("p",{children:"Consistent gap between items."})]}),n.jsxs("div",{className:"story-card",children:[n.jsx("h3",{children:"Card 4"}),n.jsx("p",{children:"Adapts to dark/light mode."})]})]})]})},c={render:()=>n.jsxs(e,{children:[n.jsx("h1",{children:"Status Badges"}),n.jsx("p",{children:"Use badges to display status or categorical information:"}),n.jsxs(s,{children:[n.jsx("h3",{children:"Badge Variants"}),n.jsxs("div",{style:{display:"flex",gap:"1rem",flexWrap:"wrap",marginTop:"1rem"},children:[n.jsx(r,{variant:"success",children:"Success"}),n.jsx(r,{variant:"warning",children:"Warning"}),n.jsx(r,{variant:"error",children:"Error"}),n.jsx(r,{variant:"info",children:"Info"})]})]}),n.jsxs(s,{children:[n.jsx("h3",{children:"In Context"}),n.jsxs("p",{children:["Build Status: ",n.jsx(r,{variant:"success",children:"Passing"})]}),n.jsxs("p",{children:["Deployment: ",n.jsx(r,{variant:"warning",children:"Pending"})]}),n.jsxs("p",{children:["Tests: ",n.jsx(r,{variant:"error",children:"5 Failed"})]}),n.jsxs("p",{children:["Coverage: ",n.jsx(r,{variant:"info",children:"87%"})]})]})]})},l={render:()=>n.jsxs(e,{size:"sm",asCard:!0,children:[n.jsx("h2",{children:"Login Form"}),n.jsx("p",{children:"This demonstrates how the CSS styles automatically apply to form elements:"}),n.jsxs("form",{style:{marginTop:"1.5rem"},onSubmit:N=>N.preventDefault(),children:[n.jsxs("div",{style:{marginBottom:"1rem"},children:[n.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500},children:"Email"}),n.jsx("input",{type:"email",placeholder:"you@example.com",style:{width:"100%",display:"block"}})]}),n.jsxs("div",{style:{marginBottom:"1rem"},children:[n.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500},children:"Password"}),n.jsx("input",{type:"password",placeholder:"••••••••",style:{width:"100%",display:"block"}})]}),n.jsx("button",{type:"submit",style:{width:"100%",marginTop:"0.5rem"},children:"Sign In"})]})]})},h={render:()=>n.jsxs(e,{children:[n.jsx("h1",{children:"Typography Showcase"}),n.jsx("p",{children:"All typography automatically adapts to the current theme."}),n.jsx(x,{}),n.jsxs(s,{children:[n.jsx("h1",{children:"Heading 1"}),n.jsx("h2",{children:"Heading 2"}),n.jsx("h3",{children:"Heading 3"}),n.jsx("h4",{children:"Heading 4"}),n.jsx("h5",{children:"Heading 5"}),n.jsx("h6",{children:"Heading 6"})]}),n.jsxs(s,{children:[n.jsxs("p",{children:["This is a paragraph with ",n.jsx("a",{href:"#",children:"a link"})," and some ",n.jsx("code",{children:"inline code"}),". The typography uses a modern font stack optimized for readability."]}),n.jsx("pre",{children:n.jsx("code",{children:`function example() {
  return "This is a code block";
}`})})]}),n.jsxs(s,{children:[n.jsxs("ul",{children:[n.jsx("li",{children:"Unordered list item 1"}),n.jsx("li",{children:"Unordered list item 2"}),n.jsx("li",{children:"Unordered list item 3"})]}),n.jsxs("ol",{children:[n.jsx("li",{children:"Ordered list item 1"}),n.jsx("li",{children:"Ordered list item 2"}),n.jsx("li",{children:"Ordered list item 3"})]})]})]})};var p,j,m;i.parameters={...i.parameters,docs:{...(p=i.parameters)==null?void 0:p.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    children: [_jsx("h1", {
      children: "Story Container Example"
    }), _jsx("p", {
      children: "This is a basic story container with default medium size (1200px max width). It provides consistent padding and centering."
    }), _jsx("p", {
      children: "The container automatically adapts to both dark and light modes using CSS variables. Try switching the background in the toolbar above!"
    })]
  })
}`,...(m=(j=i.parameters)==null?void 0:j.docs)==null?void 0:m.source}}};var g,y,u;t.parameters={...t.parameters,docs:{...(g=t.parameters)==null?void 0:g.docs,source:{originalSource:`{
  render: () => _jsxs(_Fragment, {
    children: [_jsxs(StoryContainer, {
      size: "sm",
      children: [_jsx("h2", {
        children: "Small Container"
      }), _jsx("p", {
        children: "This container has a max-width of 600px, perfect for focused content."
      })]
    }), _jsx(StoryDivider, {}), _jsxs(StoryContainer, {
      size: "md",
      children: [_jsx("h2", {
        children: "Medium Container (Default)"
      }), _jsx("p", {
        children: "This container has a max-width of 1200px, suitable for most stories."
      })]
    }), _jsx(StoryDivider, {}), _jsxs(StoryContainer, {
      size: "lg",
      children: [_jsx("h2", {
        children: "Large Container"
      }), _jsx("p", {
        children: "This container has a max-width of 1400px, for wider layouts."
      })]
    })]
  })
}`,...(u=(y=t.parameters)==null?void 0:y.docs)==null?void 0:u.source}}};var _,S,f;a.parameters={...a.parameters,docs:{...(_=a.parameters)==null?void 0:_.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    asCard: true,
    children: [_jsx("h2", {
      children: "Card Container"
    }), _jsx("p", {
      children: "This container is rendered as a card with a background, border, and shadow. Perfect for highlighting content or creating distinct sections."
    }), _jsx("p", {
      children: "Notice how the card adapts to the current theme."
    })]
  })
}`,...(f=(S=a.parameters)==null?void 0:S.docs)==null?void 0:f.source}}};var b,v,w;d.parameters={...d.parameters,docs:{...(b=d.parameters)==null?void 0:b.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    children: [_jsx("h1", {
      children: "Using Sections"
    }), _jsxs(StorySection, {
      children: [_jsx("h2", {
        children: "First Section"
      }), _jsx("p", {
        children: "Sections provide consistent vertical spacing between groups of content."
      })]
    }), _jsxs(StorySection, {
      children: [_jsx("h2", {
        children: "Second Section"
      }), _jsx("p", {
        children: "Each section automatically has margin-bottom spacing."
      })]
    }), _jsxs(StorySection, {
      children: [_jsx("h2", {
        children: "Third Section"
      }), _jsx("p", {
        children: "The last section has no bottom margin to prevent extra space."
      })]
    })]
  })
}`,...(w=(v=d.parameters)==null?void 0:v.docs)==null?void 0:w.source}}};var C,T,B;o.parameters={...o.parameters,docs:{...(C=o.parameters)==null?void 0:C.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    children: [_jsx("h1", {
      children: "Grid Layout"
    }), _jsx("p", {
      children: "Use StoryGrid for responsive grid layouts:"
    }), _jsxs(StoryGrid, {
      children: [_jsxs("div", {
        className: "story-card",
        children: [_jsx("h3", {
          children: "Card 1"
        }), _jsx("p", {
          children: "Grid items automatically wrap based on available space."
        })]
      }), _jsxs("div", {
        className: "story-card",
        children: [_jsx("h3", {
          children: "Card 2"
        }), _jsx("p", {
          children: "Minimum width of 300px per item."
        })]
      }), _jsxs("div", {
        className: "story-card",
        children: [_jsx("h3", {
          children: "Card 3"
        }), _jsx("p", {
          children: "Consistent gap between items."
        })]
      }), _jsxs("div", {
        className: "story-card",
        children: [_jsx("h3", {
          children: "Card 4"
        }), _jsx("p", {
          children: "Adapts to dark/light mode."
        })]
      })]
    })]
  })
}`,...(B=(T=o.parameters)==null?void 0:T.docs)==null?void 0:B.source}}};var k,z,U;c.parameters={...c.parameters,docs:{...(k=c.parameters)==null?void 0:k.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    children: [_jsx("h1", {
      children: "Status Badges"
    }), _jsx("p", {
      children: "Use badges to display status or categorical information:"
    }), _jsxs(StorySection, {
      children: [_jsx("h3", {
        children: "Badge Variants"
      }), _jsxs("div", {
        style: {
          display: 'flex',
          gap: '1rem',
          flexWrap: 'wrap',
          marginTop: '1rem'
        },
        children: [_jsx(StoryBadge, {
          variant: "success",
          children: "Success"
        }), _jsx(StoryBadge, {
          variant: "warning",
          children: "Warning"
        }), _jsx(StoryBadge, {
          variant: "error",
          children: "Error"
        }), _jsx(StoryBadge, {
          variant: "info",
          children: "Info"
        })]
      })]
    }), _jsxs(StorySection, {
      children: [_jsx("h3", {
        children: "In Context"
      }), _jsxs("p", {
        children: ["Build Status: ", _jsx(StoryBadge, {
          variant: "success",
          children: "Passing"
        })]
      }), _jsxs("p", {
        children: ["Deployment: ", _jsx(StoryBadge, {
          variant: "warning",
          children: "Pending"
        })]
      }), _jsxs("p", {
        children: ["Tests: ", _jsx(StoryBadge, {
          variant: "error",
          children: "5 Failed"
        })]
      }), _jsxs("p", {
        children: ["Coverage: ", _jsx(StoryBadge, {
          variant: "info",
          children: "87%"
        })]
      })]
    })]
  })
}`,...(U=(z=c.parameters)==null?void 0:z.docs)==null?void 0:U.source}}};var W,E,H;l.parameters={...l.parameters,docs:{...(W=l.parameters)==null?void 0:W.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    size: "sm",
    asCard: true,
    children: [_jsx("h2", {
      children: "Login Form"
    }), _jsx("p", {
      children: "This demonstrates how the CSS styles automatically apply to form elements:"
    }), _jsxs("form", {
      style: {
        marginTop: '1.5rem'
      },
      onSubmit: e => e.preventDefault(),
      children: [_jsxs("div", {
        style: {
          marginBottom: '1rem'
        },
        children: [_jsx("label", {
          style: {
            display: 'block',
            marginBottom: '0.5rem',
            fontWeight: 500
          },
          children: "Email"
        }), _jsx("input", {
          type: "email",
          placeholder: "you@example.com",
          style: {
            width: '100%',
            display: 'block'
          }
        })]
      }), _jsxs("div", {
        style: {
          marginBottom: '1rem'
        },
        children: [_jsx("label", {
          style: {
            display: 'block',
            marginBottom: '0.5rem',
            fontWeight: 500
          },
          children: "Password"
        }), _jsx("input", {
          type: "password",
          placeholder: "\\u2022\\u2022\\u2022\\u2022\\u2022\\u2022\\u2022\\u2022",
          style: {
            width: '100%',
            display: 'block'
          }
        })]
      }), _jsx("button", {
        type: "submit",
        style: {
          width: '100%',
          marginTop: '0.5rem'
        },
        children: "Sign In"
      })]
    })]
  })
}`,...(H=(E=l.parameters)==null?void 0:E.docs)==null?void 0:H.source}}};var D,F,G;h.parameters={...h.parameters,docs:{...(D=h.parameters)==null?void 0:D.docs,source:{originalSource:`{
  render: () => _jsxs(StoryContainer, {
    children: [_jsx("h1", {
      children: "Typography Showcase"
    }), _jsx("p", {
      children: "All typography automatically adapts to the current theme."
    }), _jsx(StoryDivider, {}), _jsxs(StorySection, {
      children: [_jsx("h1", {
        children: "Heading 1"
      }), _jsx("h2", {
        children: "Heading 2"
      }), _jsx("h3", {
        children: "Heading 3"
      }), _jsx("h4", {
        children: "Heading 4"
      }), _jsx("h5", {
        children: "Heading 5"
      }), _jsx("h6", {
        children: "Heading 6"
      })]
    }), _jsxs(StorySection, {
      children: [_jsxs("p", {
        children: ["This is a paragraph with ", _jsx("a", {
          href: "#",
          children: "a link"
        }), " and some ", _jsx("code", {
          children: "inline code"
        }), ". The typography uses a modern font stack optimized for readability."]
      }), _jsx("pre", {
        children: _jsx("code", {
          children: \`function example() {
  return "This is a code block";
}\`
        })
      })]
    }), _jsxs(StorySection, {
      children: [_jsxs("ul", {
        children: [_jsx("li", {
          children: "Unordered list item 1"
        }), _jsx("li", {
          children: "Unordered list item 2"
        }), _jsx("li", {
          children: "Unordered list item 3"
        })]
      }), _jsxs("ol", {
        children: [_jsx("li", {
          children: "Ordered list item 1"
        }), _jsx("li", {
          children: "Ordered list item 2"
        }), _jsx("li", {
          children: "Ordered list item 3"
        })]
      })]
    })]
  })
}`,...(G=(F=h.parameters)==null?void 0:F.docs)==null?void 0:G.source}}};const M=["BasicUsage","SizeVariants","CardStyle","WithSections","WithGrid","WithBadges","FormExample","Typography"];export{i as BasicUsage,a as CardStyle,l as FormExample,t as SizeVariants,h as Typography,c as WithBadges,o as WithGrid,d as WithSections,M as __namedExportsOrder,A as default};
