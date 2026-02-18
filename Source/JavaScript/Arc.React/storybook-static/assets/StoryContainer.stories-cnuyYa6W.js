import{j as e}from"./jsx-runtime-Cf8x2fCZ.js";import{S as r,a as p,b as t,c as n,d as P}from"./StoryContainer-XwC7i3zG.js";import"./index-yBjzXJbu.js";const M={title:"Stories/StoryContainer",component:r,parameters:{docs:{description:{component:"Container components for wrapping Storybook stories with consistent styling that automatically adapts to dark/light mode."}}}},i={render:()=>e.jsxs(r,{children:[e.jsx("h1",{children:"Story Container Example"}),e.jsx("p",{children:"This is a basic story container with default medium size (1200px max width). It provides consistent padding and centering."}),e.jsx("p",{children:"The container automatically adapts to both dark and light modes using CSS variables. Try switching the background in the toolbar above!"})]})},a={render:()=>e.jsxs(e.Fragment,{children:[e.jsxs(r,{size:"sm",children:[e.jsx("h2",{children:"Small Container"}),e.jsx("p",{children:"This container has a max-width of 600px, perfect for focused content."})]}),e.jsx(p,{}),e.jsxs(r,{size:"md",children:[e.jsx("h2",{children:"Medium Container (Default)"}),e.jsx("p",{children:"This container has a max-width of 1200px, suitable for most stories."})]}),e.jsx(p,{}),e.jsxs(r,{size:"lg",children:[e.jsx("h2",{children:"Large Container"}),e.jsx("p",{children:"This container has a max-width of 1400px, for wider layouts."})]})]})},s={render:()=>e.jsxs(r,{asCard:!0,children:[e.jsx("h2",{children:"Card Container"}),e.jsx("p",{children:"This container is rendered as a card with a background, border, and shadow. Perfect for highlighting content or creating distinct sections."}),e.jsx("p",{children:"Notice how the card adapts to the current theme."})]})},o={render:()=>e.jsxs(r,{children:[e.jsx("h1",{children:"Using Sections"}),e.jsxs(t,{children:[e.jsx("h2",{children:"First Section"}),e.jsx("p",{children:"Sections provide consistent vertical spacing between groups of content."})]}),e.jsxs(t,{children:[e.jsx("h2",{children:"Second Section"}),e.jsx("p",{children:"Each section automatically has margin-bottom spacing."})]}),e.jsxs(t,{children:[e.jsx("h2",{children:"Third Section"}),e.jsx("p",{children:"The last section has no bottom margin to prevent extra space."})]})]})},d={render:()=>e.jsxs(r,{children:[e.jsx("h1",{children:"Grid Layout"}),e.jsx("p",{children:"Use StoryGrid for responsive grid layouts:"}),e.jsxs(P,{children:[e.jsxs("div",{className:"story-card",children:[e.jsx("h3",{children:"Card 1"}),e.jsx("p",{children:"Grid items automatically wrap based on available space."})]}),e.jsxs("div",{className:"story-card",children:[e.jsx("h3",{children:"Card 2"}),e.jsx("p",{children:"Minimum width of 300px per item."})]}),e.jsxs("div",{className:"story-card",children:[e.jsx("h3",{children:"Card 3"}),e.jsx("p",{children:"Consistent gap between items."})]}),e.jsxs("div",{className:"story-card",children:[e.jsx("h3",{children:"Card 4"}),e.jsx("p",{children:"Adapts to dark/light mode."})]})]})]})},c={render:()=>e.jsxs(r,{children:[e.jsx("h1",{children:"Status Badges"}),e.jsx("p",{children:"Use badges to display status or categorical information:"}),e.jsxs(t,{children:[e.jsx("h3",{children:"Badge Variants"}),e.jsxs("div",{style:{display:"flex",gap:"1rem",flexWrap:"wrap",marginTop:"1rem"},children:[e.jsx(n,{variant:"success",children:"Success"}),e.jsx(n,{variant:"warning",children:"Warning"}),e.jsx(n,{variant:"error",children:"Error"}),e.jsx(n,{variant:"info",children:"Info"})]})]}),e.jsxs(t,{children:[e.jsx("h3",{children:"In Context"}),e.jsxs("p",{children:["Build Status: ",e.jsx(n,{variant:"success",children:"Passing"})]}),e.jsxs("p",{children:["Deployment: ",e.jsx(n,{variant:"warning",children:"Pending"})]}),e.jsxs("p",{children:["Tests: ",e.jsx(n,{variant:"error",children:"5 Failed"})]}),e.jsxs("p",{children:["Coverage: ",e.jsx(n,{variant:"info",children:"87%"})]})]})]})},l={render:()=>e.jsxs(r,{size:"sm",asCard:!0,children:[e.jsx("h2",{children:"Login Form"}),e.jsx("p",{children:"This demonstrates how the CSS styles automatically apply to form elements:"}),e.jsxs("form",{style:{marginTop:"1.5rem"},onSubmit:I=>I.preventDefault(),children:[e.jsxs("div",{style:{marginBottom:"1rem"},children:[e.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500},children:"Email"}),e.jsx("input",{type:"email",placeholder:"you@example.com",style:{width:"100%",display:"block"}})]}),e.jsxs("div",{style:{marginBottom:"1rem"},children:[e.jsx("label",{style:{display:"block",marginBottom:"0.5rem",fontWeight:500},children:"Password"}),e.jsx("input",{type:"password",placeholder:"••••••••",style:{width:"100%",display:"block"}})]}),e.jsx("button",{type:"submit",style:{width:"100%",marginTop:"0.5rem"},children:"Sign In"})]})]})},h={render:()=>e.jsxs(r,{children:[e.jsx("h1",{children:"Typography Showcase"}),e.jsx("p",{children:"All typography automatically adapts to the current theme."}),e.jsx(p,{}),e.jsxs(t,{children:[e.jsx("h1",{children:"Heading 1"}),e.jsx("h2",{children:"Heading 2"}),e.jsx("h3",{children:"Heading 3"}),e.jsx("h4",{children:"Heading 4"}),e.jsx("h5",{children:"Heading 5"}),e.jsx("h6",{children:"Heading 6"})]}),e.jsxs(t,{children:[e.jsxs("p",{children:["This is a paragraph with ",e.jsx("a",{href:"#",children:"a link"})," and some ",e.jsx("code",{children:"inline code"}),". The typography uses a modern font stack optimized for readability."]}),e.jsx("pre",{children:e.jsx("code",{children:`function example() {
  return "This is a code block";
}`})})]}),e.jsxs(t,{children:[e.jsxs("ul",{children:[e.jsx("li",{children:"Unordered list item 1"}),e.jsx("li",{children:"Unordered list item 2"}),e.jsx("li",{children:"Unordered list item 3"})]}),e.jsxs("ol",{children:[e.jsx("li",{children:"Ordered list item 1"}),e.jsx("li",{children:"Ordered list item 2"}),e.jsx("li",{children:"Ordered list item 3"})]})]})]})};var m,y,g;i.parameters={...i.parameters,docs:{...(m=i.parameters)==null?void 0:m.docs,source:{originalSource:`{
  render: () => <StoryContainer>
            <h1>Story Container Example</h1>
            <p>
                This is a basic story container with default medium size (1200px max width).
                It provides consistent padding and centering.
            </p>
            <p>
                The container automatically adapts to both dark and light modes using CSS variables.
                Try switching the background in the toolbar above!
            </p>
        </StoryContainer>
}`,...(g=(y=i.parameters)==null?void 0:y.docs)==null?void 0:g.source}}};var x,S,u;a.parameters={...a.parameters,docs:{...(x=a.parameters)==null?void 0:x.docs,source:{originalSource:`{
  render: () => <>
            <StoryContainer size="sm">
                <h2>Small Container</h2>
                <p>This container has a max-width of 600px, perfect for focused content.</p>
            </StoryContainer>
            
            <StoryDivider />
            
            <StoryContainer size="md">
                <h2>Medium Container (Default)</h2>
                <p>This container has a max-width of 1200px, suitable for most stories.</p>
            </StoryContainer>
            
            <StoryDivider />
            
            <StoryContainer size="lg">
                <h2>Large Container</h2>
                <p>This container has a max-width of 1400px, for wider layouts.</p>
            </StoryContainer>
        </>
}`,...(u=(S=a.parameters)==null?void 0:S.docs)==null?void 0:u.source}}};var j,f,v;s.parameters={...s.parameters,docs:{...(j=s.parameters)==null?void 0:j.docs,source:{originalSource:`{
  render: () => <StoryContainer asCard>
            <h2>Card Container</h2>
            <p>
                This container is rendered as a card with a background, border, and shadow.
                Perfect for highlighting content or creating distinct sections.
            </p>
            <p>Notice how the card adapts to the current theme.</p>
        </StoryContainer>
}`,...(v=(f=s.parameters)==null?void 0:f.docs)==null?void 0:v.source}}};var b,C,w;o.parameters={...o.parameters,docs:{...(b=o.parameters)==null?void 0:b.docs,source:{originalSource:`{
  render: () => <StoryContainer>
            <h1>Using Sections</h1>
            
            <StorySection>
                <h2>First Section</h2>
                <p>Sections provide consistent vertical spacing between groups of content.</p>
            </StorySection>
            
            <StorySection>
                <h2>Second Section</h2>
                <p>Each section automatically has margin-bottom spacing.</p>
            </StorySection>
            
            <StorySection>
                <h2>Third Section</h2>
                <p>The last section has no bottom margin to prevent extra space.</p>
            </StorySection>
        </StoryContainer>
}`,...(w=(C=o.parameters)==null?void 0:C.docs)==null?void 0:w.source}}};var T,B,k;d.parameters={...d.parameters,docs:{...(T=d.parameters)==null?void 0:T.docs,source:{originalSource:`{
  render: () => <StoryContainer>
            <h1>Grid Layout</h1>
            <p>Use StoryGrid for responsive grid layouts:</p>
            
            <StoryGrid>
                <div className="story-card">
                    <h3>Card 1</h3>
                    <p>Grid items automatically wrap based on available space.</p>
                </div>
                <div className="story-card">
                    <h3>Card 2</h3>
                    <p>Minimum width of 300px per item.</p>
                </div>
                <div className="story-card">
                    <h3>Card 3</h3>
                    <p>Consistent gap between items.</p>
                </div>
                <div className="story-card">
                    <h3>Card 4</h3>
                    <p>Adapts to dark/light mode.</p>
                </div>
            </StoryGrid>
        </StoryContainer>
}`,...(k=(B=d.parameters)==null?void 0:B.docs)==null?void 0:k.source}}};var z,U,W;c.parameters={...c.parameters,docs:{...(z=c.parameters)==null?void 0:z.docs,source:{originalSource:`{
  render: () => <StoryContainer>
            <h1>Status Badges</h1>
            <p>Use badges to display status or categorical information:</p>
            
            <StorySection>
                <h3>Badge Variants</h3>
                <div style={{
        display: 'flex',
        gap: '1rem',
        flexWrap: 'wrap',
        marginTop: '1rem'
      }}>
                    <StoryBadge variant="success">Success</StoryBadge>
                    <StoryBadge variant="warning">Warning</StoryBadge>
                    <StoryBadge variant="error">Error</StoryBadge>
                    <StoryBadge variant="info">Info</StoryBadge>
                </div>
            </StorySection>
            
            <StorySection>
                <h3>In Context</h3>
                <p>
                    Build Status: <StoryBadge variant="success">Passing</StoryBadge>
                </p>
                <p>
                    Deployment: <StoryBadge variant="warning">Pending</StoryBadge>
                </p>
                <p>
                    Tests: <StoryBadge variant="error">5 Failed</StoryBadge>
                </p>
                <p>
                    Coverage: <StoryBadge variant="info">87%</StoryBadge>
                </p>
            </StorySection>
        </StoryContainer>
}`,...(W=(U=c.parameters)==null?void 0:U.docs)==null?void 0:W.source}}};var E,H,G;l.parameters={...l.parameters,docs:{...(E=l.parameters)==null?void 0:E.docs,source:{originalSource:`{
  render: () => <StoryContainer size="sm" asCard>
            <h2>Login Form</h2>
            <p>This demonstrates how the CSS styles automatically apply to form elements:</p>
            
            <form style={{
      marginTop: '1.5rem'
    }} onSubmit={e => e.preventDefault()}>
                <div style={{
        marginBottom: '1rem'
      }}>
                    <label style={{
          display: 'block',
          marginBottom: '0.5rem',
          fontWeight: 500
        }}>
                        Email
                    </label>
                    <input type="email" placeholder="you@example.com" style={{
          width: '100%',
          display: 'block'
        }} />
                </div>
                
                <div style={{
        marginBottom: '1rem'
      }}>
                    <label style={{
          display: 'block',
          marginBottom: '0.5rem',
          fontWeight: 500
        }}>
                        Password
                    </label>
                    <input type="password" placeholder="••••••••" style={{
          width: '100%',
          display: 'block'
        }} />
                </div>
                
                <button type="submit" style={{
        width: '100%',
        marginTop: '0.5rem'
      }}>
                    Sign In
                </button>
            </form>
        </StoryContainer>
}`,...(G=(H=l.parameters)==null?void 0:H.docs)==null?void 0:G.source}}};var D,N,F;h.parameters={...h.parameters,docs:{...(D=h.parameters)==null?void 0:D.docs,source:{originalSource:`{
  render: () => <StoryContainer>
            <h1>Typography Showcase</h1>
            <p>All typography automatically adapts to the current theme.</p>
            
            <StoryDivider />
            
            <StorySection>
                <h1>Heading 1</h1>
                <h2>Heading 2</h2>
                <h3>Heading 3</h3>
                <h4>Heading 4</h4>
                <h5>Heading 5</h5>
                <h6>Heading 6</h6>
            </StorySection>
            
            <StorySection>
                <p>
                    This is a paragraph with <a href="#">a link</a> and some <code>inline code</code>.
                    The typography uses a modern font stack optimized for readability.
                </p>
                
                <pre><code>{\`function example() {
  return "This is a code block";
}\`}</code></pre>
            </StorySection>
            
            <StorySection>
                <ul>
                    <li>Unordered list item 1</li>
                    <li>Unordered list item 2</li>
                    <li>Unordered list item 3</li>
                </ul>
                
                <ol>
                    <li>Ordered list item 1</li>
                    <li>Ordered list item 2</li>
                    <li>Ordered list item 3</li>
                </ol>
            </StorySection>
        </StoryContainer>
}`,...(F=(N=h.parameters)==null?void 0:N.docs)==null?void 0:F.source}}};const V=["BasicUsage","SizeVariants","CardStyle","WithSections","WithGrid","WithBadges","FormExample","Typography"];export{i as BasicUsage,s as CardStyle,l as FormExample,a as SizeVariants,h as Typography,c as WithBadges,d as WithGrid,o as WithSections,V as __namedExportsOrder,M as default};
